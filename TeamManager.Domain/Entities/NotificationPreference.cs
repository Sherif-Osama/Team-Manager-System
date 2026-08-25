using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;

namespace TeamManager.Domain.Entities;

public class NotificationPreference : Entity<(Guid UserId, NotificationType NotificationType)>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public NotificationType NotificationType { get; private set; }
    public bool IsEnabled { get; private set; }

    private NotificationPreference() { }

    public NotificationPreference(Guid userId, NotificationType notificationType, bool isEnabled = true)
    {
        UserId = userId;
        NotificationType = notificationType;
        IsEnabled = isEnabled;
        Id = (userId, notificationType);
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
