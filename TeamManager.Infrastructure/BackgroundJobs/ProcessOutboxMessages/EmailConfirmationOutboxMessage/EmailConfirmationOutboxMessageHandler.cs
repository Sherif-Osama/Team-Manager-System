using System.Text.Json;
using TeamManager.Application.Common.Outbox;
using TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.OutboxMessages;
using TeamManager.Infrastructure.Communication;

namespace TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.EmailConfirmationOutboxMessage
{
    public sealed class EmailConfirmationOutboxMessageHandler(IEmailSender emailSender) : IOutboxMessageHandler
    {
        private sealed record Payload(string To, string Token);

        public OutboxMessageType Type => OutboxMessageType.EmailConfirmationEmail;

        public async Task HandleAsync(string payload, CancellationToken cancellationToken)
        {
            var data = JsonSerializer.Deserialize<Payload>(payload)
                ?? throw new InvalidOperationException("Invalid email confirmation payload.");

            await emailSender.SendEmailConfirmationAsync(data.To, data.Token, cancellationToken);
        }
    }
}