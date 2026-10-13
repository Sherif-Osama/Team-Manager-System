using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class NotificationRepository(TeamManagerDbContext context) : INotificationRepository
    {
        public async Task AddNotificationAsync(Notification notification, CancellationToken cancellationToken)
        {
            await context.Notifications.AddAsync(notification, cancellationToken);
        }

        public async Task<Notification?> GetByIdAsync(long notificationId, CancellationToken cancellationToken)
        {
            return await context.Notifications.FindAsync([notificationId], cancellationToken);
        }

        public async Task<NotificationPreference?> GetPreferenceAsync(Guid userId, NotificationType notificationType,
            CancellationToken cancellationToken)
        {
            return await context.NotificationPreferences.FirstOrDefaultAsync(x => x.UserId == userId && x.User.DeletedAtUtc == null
            && x.NotificationType == notificationType, cancellationToken);
        }

        public async Task<IReadOnlyCollection<Notification>> GetUnreadByRecipientIdAsync(Guid recipientUserId, CancellationToken cancellationToken)
        {
            return await context.Notifications.Where(x => x.RecipientUserId == recipientUserId && !x.IsRead)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<NotificationPreference>> GetPreferencesByUserIdAsync(Guid userId,
            CancellationToken cancellationToken)
        {
            return await context.NotificationPreferences.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        }

        public async Task AddPreferenceAsync(NotificationPreference preference, CancellationToken cancellationToken)
        {
            await context.NotificationPreferences.AddAsync(preference, cancellationToken);
        }
    }
}