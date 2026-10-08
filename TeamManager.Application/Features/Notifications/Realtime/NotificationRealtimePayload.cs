using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Notifications.Realtime
{
    public sealed record NotificationRealtimePayload(long NotificationId, NotificationType Type, string Title, string? Body,
        string? RelatedEntityType, string? RelatedEntityId, DateTime CreatedAtUtc);
}