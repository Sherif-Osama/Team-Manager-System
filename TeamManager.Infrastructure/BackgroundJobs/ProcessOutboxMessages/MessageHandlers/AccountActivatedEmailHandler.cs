using System.Text.Json;
using TeamManager.Application.Common.Outbox;
using TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.OutboxMessages;
using TeamManager.Infrastructure.Communication;

namespace TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.MessageHandlers
{
    public sealed class AccountActivatedEmailHandler(IEmailSender emailSender) : IOutboxMessageHandler
    {
        private sealed record Payload(string To, DateTime ActivatedAtUtc, string? DeviceInfo);

        public OutboxMessageType Type => OutboxMessageType.AccountActivationEmail;

        public async Task HandleAsync(string payload, CancellationToken cancellationToken)
        {
            var accountActivatedEmail = JsonSerializer.Deserialize<Payload>(payload)
                ?? throw new InvalidOperationException("Failed to deserialize payload.");

            await emailSender.SendAccountActivatedAsync(accountActivatedEmail.To, accountActivatedEmail.ActivatedAtUtc,
                accountActivatedEmail.DeviceInfo, cancellationToken);
        }
    }
}