using TeamManager.Domain.Enums;

namespace TeamManager.Application.Abstractions.Notifications
{
    public interface INotificationWriter
    {
        Task<bool> CreateIfEnabledAsync(Guid recipientUserId, NotificationType type, string title, string? body,
            string? relatedEntityType, string? relatedEntityId, CancellationToken cancellationToken);
    }
}