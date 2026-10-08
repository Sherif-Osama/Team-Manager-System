using TeamManager.Application.Features.Notifications.Realtime;

namespace TeamManager.Application.Abstractions.Realtime
{
    public interface IRealtimeNotifier
    {
        Task NotifyUserAsync(Guid userId, NotificationRealtimePayload payload, CancellationToken cancellationToken);
    }
}