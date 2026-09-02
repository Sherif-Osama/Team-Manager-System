using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Outbox;

namespace TeamManager.Infrastructure.Persistence.Outbox
{
    public sealed class Outbox(TeamManagerDbContext context) : IOutbox
    {
        public void Add(OutboxMessageType type, string payload)
        {
            context.OutboxMessages.Add(new OutboxMessage
            {
                Type = type.ToString(),
                Payload = payload,
                OccurredOnUtc = DateTime.UtcNow,
                RetryCount = 0,
                NextAttemptOnUtc = DateTime.UtcNow
            });
        }
    }
}