using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.UnitTest.Domain
{
    /// <summary>
    /// Tests Notification and NotificationPreference directly because they are
    /// standalone entities created and mutated in handlers, outside any aggregate.
    /// </summary>
    public sealed class NotificationTests
    {
        #region Notification
        [Fact]
        public void Constructor_WithValidData_SetsFields()
        {
            var recipient = Guid.NewGuid();
            var before = DateTime.UtcNow;

            var notification = new Notification(recipient, NotificationType.TaskAssigned, "Task Assigned");

            Assert.Equal(recipient, notification.RecipientUserId);
            Assert.Equal(NotificationType.TaskAssigned, notification.Type);
            Assert.Equal("Task Assigned", notification.Title);
            Assert.Null(notification.Body);
            Assert.Null(notification.RelatedEntityType);
            Assert.Null(notification.RelatedEntityId);
            Assert.False(notification.IsRead);
            Assert.Null(notification.ReadAtUtc);
            Assert.InRange(notification.CreatedAtUtc, before, DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_WithBodyAndRelatedEntity_SetsThem()
        {
            var notification = new Notification(Guid.NewGuid(), NotificationType.Mention,
                "You were mentioned", "Someone mentioned you", "Task", "42");

            Assert.Equal("Someone mentioned you", notification.Body);
            Assert.Equal("Task", notification.RelatedEntityType);
            Assert.Equal("42", notification.RelatedEntityId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankTitle_Throws(string? title)
        {
            Assert.Throws<DomainException>(() =>
                new Notification(Guid.NewGuid(), NotificationType.TaskAssigned, title!));
        }
        #endregion

        #region Mark As Read
        [Fact]
        public void MarkAsRead_WhenUnread_SetsReadAndTimestamp()
        {
            var notification = new Notification(Guid.NewGuid(), NotificationType.TaskAssigned, "Task Assigned");
            var before = DateTime.UtcNow;

            notification.MarkAsRead();

            Assert.True(notification.IsRead);
            Assert.NotNull(notification.ReadAtUtc);
            Assert.InRange(notification.ReadAtUtc!.Value, before, DateTime.UtcNow);
        }

        [Fact]
        public void MarkAsRead_WhenAlreadyRead_KeepsOriginalTimestamp()
        {
            var notification = new Notification(Guid.NewGuid(), NotificationType.TaskAssigned, "Task Assigned");
            notification.MarkAsRead();
            var firstReadAt = notification.ReadAtUtc;

            notification.MarkAsRead();

            Assert.True(notification.IsRead);
            Assert.Equal(firstReadAt, notification.ReadAtUtc);
        }
        #endregion

        #region Notification Preference
        [Fact]
        public void Constructor_DefaultsToEnabled()
        {
            var userId = Guid.NewGuid();

            var preference = new NotificationPreference(userId, NotificationType.TaskAssigned);

            Assert.Equal(userId, preference.UserId);
            Assert.Equal(NotificationType.TaskAssigned, preference.NotificationType);
            Assert.Equal((userId, NotificationType.TaskAssigned), preference.Id);
            Assert.True(preference.IsEnabled);
        }

        [Fact]
        public void Constructor_WithIsEnabledFalse_StartsDisabled()
        {
            var preference = new NotificationPreference(Guid.NewGuid(), NotificationType.Mention, false);

            Assert.False(preference.IsEnabled);
        }

        [Fact]
        public void Disable_ThenEnable_TogglesCorrectly()
        {
            var preference = new NotificationPreference(Guid.NewGuid(), NotificationType.TaskAssigned);

            preference.Disable();
            Assert.False(preference.IsEnabled);

            preference.Enable();
            Assert.True(preference.IsEnabled);
        }
        #endregion
    }
}
