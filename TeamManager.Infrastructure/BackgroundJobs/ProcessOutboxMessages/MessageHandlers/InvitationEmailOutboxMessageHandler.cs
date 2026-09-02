using System.Text.Json;
using TeamManager.Application.Common.Outbox;
using TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.OutboxMessages;
using TeamManager.Infrastructure.Communication;

namespace TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.MessageHandlers
{
    public sealed class InvitationEmailOutboxMessageHandler(IEmailSender emailSender) : IOutboxMessageHandler
    {
        private sealed record Payload(string To, string Token);

        public OutboxMessageType Type => OutboxMessageType.InvitationEmail;

        public async Task HandleAsync(string payload, CancellationToken cancellationToken)
        {
            var data = JsonSerializer.Deserialize<Payload>(payload)
                ?? throw new InvalidOperationException("Invalid invitation email payload.");

            await emailSender.SendInvitationEmailAsync(data.To, data.Token, cancellationToken);
        }
    }
}
