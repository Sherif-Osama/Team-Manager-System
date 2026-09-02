using System.Text.Json;
using TeamManager.Application.Common.Outbox;
using TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.OutboxMessages;
using TeamManager.Infrastructure.Communication;

namespace TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.MessageHandlers
{
    public sealed class AccountDeletedEmailHandler(IEmailSender emailSender) : IOutboxMessageHandler
    {
        private sealed record Payload(string To, DateTime DeletedAtUtc, string? DeviceInfo);

        public OutboxMessageType Type => OutboxMessageType.AccountDeletedEmail;

        public Task HandleAsync(string payload, CancellationToken cancellationToken)
        {
            var accountDeletedEmail = JsonSerializer.Deserialize<Payload>(payload)
                ??
                throw new InvalidOperationException("Invalid account deleted email payload");

            return emailSender.SendAccountDeletedAsync(accountDeletedEmail.To, accountDeletedEmail.DeletedAtUtc,
                accountDeletedEmail.DeviceInfo, cancellationToken);
        }
    }
}