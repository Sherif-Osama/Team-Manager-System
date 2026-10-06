using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(long notificationId, CancellationToken cancellationToken);
    }
}