using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Infrastructure.Persistence.Outbox
{
    public sealed class Outbox(TeamManagerDbContext context) : IOutbox
    {
        public void Add(string type, string payload)
        {
            context.OutboxMessages.Add(new OutboxMessage
            {
                Type = type,
                Payload = payload,
                OccurredOnUtc = DateTime.UtcNow,
                RetryCount = 0,
                NextAttemptOnUtc = DateTime.UtcNow
            });
        }
    }
}