using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Common.Outbox;
using TeamManager.Infrastructure.Persistence;
using TeamManager.Infrastructure.Persistence.Outbox;
namespace TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.OutboxMessages
{
    public sealed class OutboxProcessorService(TeamManagerDbContext context, IEnumerable<IOutboxMessageHandler> handlers)
    {
        private const int MaxRetries = 5;
        private readonly Dictionary<OutboxMessageType, IOutboxMessageHandler> _handlers = handlers.ToDictionary(h => h.Type);

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            var messages = await context.OutboxMessages.Where(x =>
                x.ProcessedOnUtc == null && x.FailedOnUtc == null
                && x.NextAttemptOnUtc <= DateTime.UtcNow).OrderBy(x => x.OccurredOnUtc).Take(20).ToListAsync(cancellationToken);

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
                return;
            }

            try
            {
                await handler.HandleAsync(message.Payload, cancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
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
                    message.FailedOnUtc = DateTime.UtcNow;
                else
                    message.NextAttemptOnUtc = DateTime.UtcNow.AddMinutes(Math.Pow(2, message.RetryCount - 1));
            }
        }
    }
}