using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class NotificationRepository(TeamManagerDbContext context) : INotificationRepository
    {
        public async Task<Notification?> GetByIdAsync(long notificationId, CancellationToken cancellationToken)
        {
            return await context.Notifications.FindAsync(notificationId, cancellationToken);
        }

        public async Task<IReadOnlyCollection<Notification>> GetUnreadByRecipientIdAsync(Guid recipientUserId, CancellationToken cancellationToken)
        {
            return await context.Notifications.Where(x => x.RecipientUserId == recipientUserId && !x.IsRead)
                .ToListAsync(cancellationToken);
        }
    }
}