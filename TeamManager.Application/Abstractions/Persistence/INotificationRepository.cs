using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(long notificationId, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Notification>> GetUnreadByRecipientIdAsync(Guid recipientUserId, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<NotificationPreference>> GetPreferencesByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task AddPreferenceAsync(NotificationPreference preference, CancellationToken cancellationToken);
    }
}