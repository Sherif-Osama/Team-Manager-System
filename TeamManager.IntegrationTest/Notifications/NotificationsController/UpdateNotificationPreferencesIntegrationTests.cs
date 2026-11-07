using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Notifications.Configuration;
using TeamManager.IntegrationTest.Projects.Configuration;

namespace TeamManager.IntegrationTest.Notifications.NotificationsController
{
    [Collection("Common")]
    public sealed class UpdateNotificationPreferencesIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly NotificationTestHelpers _notifications = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task UpdateNotificationPreferences_Returns204AndStoresTheDisabledType()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("update-disable");

            var response = await _notifications.UpdatePreferencesRequestAsync(
                new { preferences = new[] { new { notificationType = NotificationType.Mention, isEnabled = false } } }, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());

            Assert.False((await _notifications.GetPreferencesResultAsync(ownerToken)).Items
                .Single(x => x.NotificationType == NotificationType.Mention).IsEnabled);
        }

        [Fact]
        public async Task UpdateNotificationPreferences_ReEnablingAStoredType_UpdatesTheSameRow()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("update-enable");

            await _notifications.UpdatePreferencesAsync(ownerToken, (NotificationType.Mention, false));
            await _notifications.UpdatePreferencesAsync(ownerToken, (NotificationType.Mention, true));

            var items = (await _notifications.GetPreferencesResultAsync(ownerToken)).Items.ToArray();

            Assert.Equal(5, items.Length);
            Assert.Single(items, x => x.NotificationType == NotificationType.Mention);
            Assert.True(items.Single(x => x.NotificationType == NotificationType.Mention).IsEnabled);
        }

        [Fact]
        public async Task UpdateNotificationPreferences_WithSeveralTypes_StoresAllOfThem()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("update-several");

            await _notifications.UpdatePreferencesAsync(ownerToken, (NotificationType.Mention, false),
                (NotificationType.TaskCompleted, false), (NotificationType.CommentAdded, true));

            var items = (await _notifications.GetPreferencesResultAsync(ownerToken)).Items;

            Assert.False(items.Single(x => x.NotificationType == NotificationType.Mention).IsEnabled);
            Assert.False(items.Single(x => x.NotificationType == NotificationType.TaskCompleted).IsEnabled);
            Assert.True(items.Single(x => x.NotificationType == NotificationType.CommentAdded).IsEnabled);
            Assert.True(items.Single(x => x.NotificationType == NotificationType.TaskAssigned).IsEnabled);
        }

        [Fact]
        public async Task UpdateNotificationPreferences_DisablingTaskAssigned_StopsTheAssignmentNotification()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("update-gate-assign");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Gated Assignee", ownerToken);

            await _notifications.UpdatePreferencesAsync(memberToken, (NotificationType.TaskAssigned, false));

            var taskId = await _helpers.CreateTaskAsync(projectId, "Gated assigned task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            Assert.Empty((await _notifications.GetNotificationsResultAsync(memberToken)).Items);
            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task UpdateNotificationPreferences_ReEnablingTaskAssigned_CreatesTheNotificationAgain()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("update-gate-again");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Reopened Assignee", ownerToken);

            await _notifications.UpdatePreferencesAsync(memberToken, (NotificationType.TaskAssigned, false));

            var firstTaskId = await _helpers.CreateTaskAsync(projectId, "Silent assigned task", ownerToken);
            await _helpers.AssignTaskAsync(firstTaskId, memberId, ownerToken);

            Assert.Empty((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            await _notifications.UpdatePreferencesAsync(memberToken, (NotificationType.TaskAssigned, true));

            var secondTaskId = await _helpers.CreateTaskAsync(projectId, "Loud assigned task", ownerToken);
            await _helpers.AssignTaskAsync(secondTaskId, memberId, ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            Assert.Equal(secondTaskId.ToString(), item.RelatedEntityId);
        }

        [Fact]
        public async Task UpdateNotificationPreferences_DisablingMention_KeepsTheMentionRowInTheCommentAndTheMentionsFeed()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("update-gate-mention");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Muted Mentionee", ownerToken);

            await _notifications.UpdatePreferencesAsync(memberToken, (NotificationType.Mention, false));

            var taskId = await _helpers.CreateTaskAsync(projectId, "Muted mention task", ownerToken);
            var commentId = await _helpers.AddTaskCommentAsync(taskId, "Still pinging you", ownerToken, new[] { memberId });

            Assert.Empty((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            var comment = Assert.Single((await _helpers.GetTaskCommentsResultAsync(taskId, memberToken)).Items);

            Assert.Equal(commentId, comment.Id);
            Assert.Equal(new[] { memberId }, comment.Mentions.Select(x => x.UserId).ToArray());

            var mention = Assert.Single((await _helpers.GetMyMentionsResultAsync(memberToken)).Items);

            Assert.Equal(commentId, mention.CommentId);
        }

        [Fact]
        public async Task UpdateNotificationPreferences_DisablingTeamInvitation_StopsTheInvitationNotification()
        {
            var (teamId, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("update-gate-invite");
            var (_, invitedEmail, invitedToken) = await _authenticatedUser.CreateConfirmedUserAsync("update-gate-invited");

            await _notifications.UpdatePreferencesAsync(invitedToken, (NotificationType.TeamInvitation, false));

            await _notifications.InviteRegisteredUserAsync(teamId, invitedEmail, ownerToken);

            Assert.Empty((await _notifications.GetNotificationsResultAsync(invitedToken)).Items);
        }

        [Fact]
        public async Task UpdateNotificationPreferences_DisablingAType_LeavesTheAlreadyCreatedNotificationsInPlace()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("update-keeps");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Kept Notification Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Task before muting", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            await _notifications.UpdatePreferencesAsync(memberToken, (NotificationType.TaskAssigned, false));

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            Assert.Equal(taskId.ToString(), item.RelatedEntityId);
            Assert.Equal(1, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task UpdateNotificationPreferences_AsOneUser_DoesNotChangeThePreferencesOfAnother()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("update-peruser");
            var (mutedId, mutedToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Muting Member", ownerToken);
            var (normalId, normalToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Default Member", ownerToken);

            await _notifications.UpdatePreferencesAsync(mutedToken, (NotificationType.Mention, false));

            Assert.True((await _notifications.GetPreferencesResultAsync(normalToken)).Items
                .Single(x => x.NotificationType == NotificationType.Mention).IsEnabled);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Shared ping task", ownerToken);
            await _helpers.AddTaskCommentAsync(taskId, "Pinging both of you", ownerToken, new[] { mutedId, normalId });

            Assert.Empty((await _notifications.GetNotificationsResultAsync(mutedToken)).Items);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(normalToken)).Items);

            Assert.Equal(NotificationType.Mention, item.Type);
            Assert.Equal(1, await _notifications.GetUnreadCountResultAsync(normalToken));
        }




        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(99)]
        public async Task UpdateNotificationPreferences_WithATypeOutsideTheEnum_Returns400FieldError(int notificationType)
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("update-badtype");

            await ValidationAssertions.AssertFieldErrorAsync(await _notifications.UpdatePreferencesRequestAsync(
                new { preferences = new[] { new { notificationType, isEnabled = false } } }, ownerToken), "NotificationType");

            Assert.All((await _notifications.GetPreferencesResultAsync(ownerToken)).Items, x => Assert.True(x.IsEnabled));
        }


        [Fact]
        public async Task UpdateNotificationPreferences_WithoutTheIsEnabledField_DisablesTheType()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("update-noisenabled");

            var response = await _notifications.UpdatePreferencesRequestAsync(
                new { preferences = new[] { new { notificationType = NotificationType.TaskCompleted } } }, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var item = (await _notifications.GetPreferencesResultAsync(ownerToken)).Items
                .Single(x => x.NotificationType == NotificationType.TaskCompleted);

            Assert.False(item.IsEnabled);
        }

        [Fact]
        public async Task UpdateNotificationPreferences_WithoutAToken_Returns401()
        {
            var response = await _notifications.UpdatePreferencesRequestAsync(
                new { preferences = new[] { new { notificationType = NotificationType.Mention, isEnabled = false } } });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
