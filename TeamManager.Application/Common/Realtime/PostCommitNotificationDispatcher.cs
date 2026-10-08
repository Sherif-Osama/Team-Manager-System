using TeamManager.Application.Abstractions.Realtime;
using TeamManager.Application.Features.Notifications.Realtime;
using TeamManager.Domain.Entities;

namespace TeamManager.Application.Common.Realtime
{
    public sealed class PostCommitNotificationDispatcher(IRealtimeNotifier realtimeNotifier) : IPostCommitNotificationDispatcher
    {
        private readonly List<Notification> _pendingNotifications = [];

        public void Enqueue(Notification notification)
        {
            _pendingNotifications.Add(notification);
        }

        public async Task DispatchAsync(CancellationToken cancellationToken)
        {
            if (_pendingNotifications.Count == 0)
                return;

            var notifications = _pendingNotifications.ToList();

            _pendingNotifications.Clear();

            foreach (var notification in notifications)
            {
                var payload = new NotificationRealtimePayload(notification.Id, notification.Type, notification.Title, notification.Body,
                    notification.RelatedEntityType, notification.RelatedEntityId, notification.CreatedAtUtc);

                try
                {
                    await realtimeNotifier.NotifyUserAsync(notification.RecipientUserId, payload, cancellationToken);
                }
                catch
                {
                    // Realtime delivery is best-effort.
                    // The notification is already persisted in the database.
                }
            }
        }

        public void Discard()
        {
            _pendingNotifications.Clear();
        }
    }
}
