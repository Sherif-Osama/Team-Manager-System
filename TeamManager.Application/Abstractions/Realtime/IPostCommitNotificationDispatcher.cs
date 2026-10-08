using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Realtime
{
    public interface IPostCommitNotificationDispatcher
    {
        void Enqueue(Notification notification);

        Task DispatchAsync(CancellationToken cancellationToken);

        void Discard();
    }
}