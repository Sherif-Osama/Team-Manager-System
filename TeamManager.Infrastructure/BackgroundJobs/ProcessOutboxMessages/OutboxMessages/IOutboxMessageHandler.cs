using TeamManager.Application.Common.Outbox;

namespace TeamManager.Infrastructure.BackgroundJobs.ProcessOutboxMessages.OutboxMessages
{
    public interface IOutboxMessageHandler
    {
        OutboxMessageType Type { get; }
        Task HandleAsync(string payload, CancellationToken cancellationToken);
    }
};