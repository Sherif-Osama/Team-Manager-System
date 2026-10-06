namespace TeamManager.Application.Common.Exceptions.NotificationExceptions
{
    public sealed class NotificationNotFoundException : ApplicationExceptionBase
    {
        public NotificationNotFoundException(long notificationId)
            : base($"Notification with ID {notificationId} was not found.") { }
    }
}