using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Outbox;

namespace TeamManager.Infrastructure.Persistence.Outbox
{
    public sealed class Outbox(TeamManagerDbContext context) : IOutbox
    {
        public async Task Add(OutboxMessageType type, string payload)
        {
            await context.OutboxMessages.AddAsync(new OutboxMessage
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