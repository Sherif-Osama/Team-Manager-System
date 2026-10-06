using MediatR;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Notifications.Commands.UpdateNotificationPreferences
{
    public sealed record UpdateNotificationPreferencesCommand(IReadOnlyCollection<NotificationPreferenceItem> Preferences)
        : IRequest;

    public sealed record NotificationPreferenceItem(NotificationType NotificationType, bool IsEnabled);
}