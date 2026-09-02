using TeamManager.Application.Common.Outbox;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IOutbox
    {
        void Add(OutboxMessageType type, string payload);
    }
}