using System.Text.Json;
using TeamManager.Application.Common.Outbox;
using TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.OutboxMessages;
using TeamManager.Infrastructure.Communication;

namespace TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.MessageHandlers
{
    public sealed class AccountDeactivatedEmailHandler(IEmailSender emailSender) : IOutboxMessageHandler
    {
        private sealed record Payload(string To, DateTime DeactivatedAtUtc, string? DeviceInfo);
        public OutboxMessageType Type => OutboxMessageType.AccountDeactivatedEmail;

        public async Task HandleAsync(string payload, CancellationToken cancellationToken)
        {
            var accountDeactivatedEmail = JsonSerializer.Deserialize<Payload>(payload)
                ?? throw new InvalidOperationException("Failed to deserialize payload");

            await emailSender.SendAccountDeactivatedAsync(accountDeactivatedEmail.To, accountDeactivatedEmail.DeactivatedAtUtc,
                accountDeactivatedEmail.DeviceInfo, cancellationToken);
        }
    }
}