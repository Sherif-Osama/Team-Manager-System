using TeamManager.Application.Common.Outbox;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IOutbox
    {
        Task Add(OutboxMessageType type, string payload);
    }
}