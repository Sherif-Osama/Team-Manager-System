using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(long notificationId, CancellationToken cancellationToken);
        Task<NotificationPreference?> GetPreferenceAsync(Guid userId, NotificationType notificationType, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Notification>> GetUnreadByRecipientIdAsync(Guid recipientUserId, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<NotificationPreference>> GetPreferencesByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task AddPreferenceAsync(NotificationPreference preference, CancellationToken cancellationToken);
        Task AddNotificationAsync(Notification notification, CancellationToken cancellationToken);
    }
}