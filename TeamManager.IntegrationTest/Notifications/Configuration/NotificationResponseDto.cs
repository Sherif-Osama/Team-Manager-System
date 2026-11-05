using TeamManager.Domain.Enums;

namespace TeamManager.IntegrationTest.Notifications.Configuration
{
    public sealed record GetMyNotificationsResponseDto(IReadOnlyList<NotificationItemDto> Items, int TotalCount, int Page,
        int PageSize);

    public sealed record NotificationItemDto(long Id, NotificationType Type, string Title, string? Body,
        string? RelatedEntityType, string? RelatedEntityId, bool IsRead, DateTime? ReadAtUtc, DateTime CreatedAtUtc);

    public sealed record GetMyNotificationPreferencesResponseDto(IReadOnlyList<NotificationPreferenceItemDto> Items);

    public sealed record NotificationPreferenceItemDto(NotificationType NotificationType, bool IsEnabled);
}
