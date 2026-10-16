using TeamManager.Application.Abstractions.Notifications;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Infrastructure.Services.Notifications
{
    public sealed class NotificationWriter(INotificationRepository notificationRepository) : INotificationWriter
    {
        public async Task<bool> CreateIfEnabledAsync(Guid recipientUserId, NotificationType type, string title, string? body,
            string? relatedEntityType, string? relatedEntityId, CancellationToken cancellationToken)
        {
            var preference = await notificationRepository.GetPreferenceAsync(recipientUserId, type, cancellationToken);

            if (preference is { IsEnabled: false })
                return false;

            var notification = new Notification(recipientUserId, type, title, body, relatedEntityType, relatedEntityId);

            await notificationRepository.AddNotificationAsync(notification, cancellationToken);

            return true;
        }
    }
}