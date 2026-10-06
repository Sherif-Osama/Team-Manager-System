using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Notifications.Queries.GetMyNotificationPreferences
{
    public sealed record GetMyNotificationPreferencesResponse(IReadOnlyCollection<GetMyNotificationPreferenceItem> Items);

    public sealed record GetMyNotificationPreferenceItem(NotificationType NotificationType, bool IsEnabled);
}