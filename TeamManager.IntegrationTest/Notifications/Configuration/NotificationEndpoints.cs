namespace TeamManager.IntegrationTest.Notifications.Configuration
{
    static class NotificationEndpoints
    {
        public const string Notifications = "/api/notifications";
        public const string UnreadCount = "/api/notifications/unread-count";
        public const string ReadAll = "/api/notifications/read-all";
        public const string Preferences = "/api/notifications/preferences";

        public static string Read(long notificationId) => $"/api/notifications/{notificationId}/read";
    }
}
