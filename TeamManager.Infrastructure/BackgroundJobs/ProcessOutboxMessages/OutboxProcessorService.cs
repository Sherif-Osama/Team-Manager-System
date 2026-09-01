using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TeamManager.Infrastructure.Communication;
using TeamManager.Infrastructure.Persistence;

namespace TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages
{
    public sealed class OutboxProcessorService(TeamManagerDbContext context, IEmailSender emailSender)
    {
        private const int MaxRetries = 5;

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            var messages = await context.OutboxMessages.Where(x =>
                x.ProcessedOnUtc == null && x.FailedOnUtc == null
                && x.NextAttemptOnUtc <= DateTime.UtcNow).OrderBy(x => x.OccurredOnUtc).Take(20).ToListAsync(cancellationToken);

            foreach (var message in messages)
            {
                try
                {
                    if (message.Type != "Invitation Email")
                    {
                        message.FailedOnUtc = DateTime.UtcNow;
                        message.Error = $"Unsupported outbox message type: {message.Type}";
                        continue;
                    }

                    var payload = JsonSerializer.Deserialize<InvitationEmailPayload>(message.Payload);

                    if (payload is null)
                    {
                        message.FailedOnUtc = DateTime.UtcNow;
                        message.Error = "Invalid invitation email payload.";
                        continue;
                    }

                    await emailSender.SendInvitationEmailAsync(payload.To, payload.Token, cancellationToken);

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
                    {
                        message.FailedOnUtc = DateTime.UtcNow;
                    }
                    else
                    {
                        message.NextAttemptOnUtc = DateTime.UtcNow.AddMinutes(Math.Pow(2, message.RetryCount - 1));
                    }
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
    internal sealed record InvitationEmailPayload(string To, string Token, Guid InvitedBy);
}