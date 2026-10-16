using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Common.Outbox;
using TeamManager.Infrastructure.Persistence;
using TeamManager.Infrastructure.Persistence.Outbox;
namespace TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.OutboxMessages
{
    public sealed class OutboxProcessorService(TeamManagerDbContext context, IEnumerable<IOutboxMessageHandler> handlers)
    {
        private const int MaxRetries = 5;
        private const int BatchSize = 20;
        private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
        private readonly Dictionary<OutboxMessageType, IOutboxMessageHandler> _handlers = handlers.ToDictionary(h => h.Type);

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var leaseId = Guid.NewGuid();
            var leaseExpiresAtUtc = now.Add(LeaseDuration);

            var candidateIds = await context.OutboxMessages.AsNoTracking().Where(message =>
                    message.ProcessedOnUtc == null && message.FailedOnUtc == null &&
                    message.NextAttemptOnUtc <= now &&
                    (message.LeaseExpiresAtUtc == null || message.LeaseExpiresAtUtc <= now))
                .OrderBy(message => message.OccurredOnUtc)
                .Take(BatchSize)
                .Select(message => message.Id)
                .ToListAsync(cancellationToken);

            foreach (var candidateId in candidateIds)
            {
                await context.OutboxMessages
                    .Where(message => message.Id == candidateId &&
                        message.ProcessedOnUtc == null && message.FailedOnUtc == null &&
                        message.NextAttemptOnUtc <= now &&
                        (message.LeaseExpiresAtUtc == null || message.LeaseExpiresAtUtc <= now))
                    .ExecuteUpdateAsync(updates => updates
                        .SetProperty(message => message.LeaseId, leaseId)
                        .SetProperty(message => message.LeaseExpiresAtUtc, leaseExpiresAtUtc), cancellationToken);
            }

            var messages = await context.OutboxMessages
                .Where(message => message.LeaseId == leaseId)
                .OrderBy(message => message.OccurredOnUtc)
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
            {
                await ProcessOneAsync(message, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private async Task ProcessOneAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<OutboxMessageType>(message.Type, out var type) || !_handlers.TryGetValue(type, out var handler))
            {
                message.FailedOnUtc = DateTime.UtcNow;
                message.Error = $"No handler registered for outbox message type '{message.Type}'.";
                message.Payload = "{}";
                ReleaseLease(message);
                return;
            }

            try
            {
                await handler.HandleAsync(message.Payload, cancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Payload = "{}";
                ReleaseLease(message);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                if (message.RetryCount >= MaxRetries)
                {
                    message.FailedOnUtc = DateTime.UtcNow;
                    message.Payload = "{}";
                }
                else
                    message.NextAttemptOnUtc = DateTime.UtcNow.AddMinutes(Math.Pow(2, message.RetryCount - 1));

                ReleaseLease(message);
            }
        }

        private static void ReleaseLease(OutboxMessage message)
        {
            message.LeaseId = null;
            message.LeaseExpiresAtUtc = null;
        }
    }
}
