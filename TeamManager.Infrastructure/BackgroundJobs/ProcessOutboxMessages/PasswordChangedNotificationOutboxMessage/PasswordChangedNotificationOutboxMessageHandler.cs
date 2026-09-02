using System.Text.Json;
using TeamManager.Application.Common.Outbox;
using TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.OutboxMessages;
using TeamManager.Infrastructure.Communication;

namespace TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.PasswordChangedNotificationOutboxMessage
{
    public sealed class PasswordChangedNotificationOutboxMessageHandler(IEmailSender emailSender) : IOutboxMessageHandler
    {
        private sealed record Payload(string To);

        public OutboxMessageType Type => OutboxMessageType.PasswordChangedNotificationEmail;

        public async Task HandleAsync(string payload, CancellationToken cancellationToken)
        {
            var data = JsonSerializer.Deserialize<Payload>(payload)
                ?? throw new InvalidOperationException("Invalid password changed payload.");

            await emailSender.SendPasswordChangedNotificationAsync(data.To, cancellationToken);
        }
    }
}