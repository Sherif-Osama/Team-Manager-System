using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Notifications.Configuration;
using TeamManager.IntegrationTest.Projects.Configuration;

namespace TeamManager.IntegrationTest.Notifications.NotificationsController
{
    [Collection("Projects")]
    public sealed class MarkAllAsReadIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly NotificationTestHelpers _notifications = new(factory);

        [Fact]
        public async Task MarkAllAsRead_ReadsEveryUnreadNotificationOfTheRecipient()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("readall-all");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "ReadAll Member", ownerToken);

            var assignedTaskId = await _helpers.CreateTaskAsync(projectId, "Read all assigned task", ownerToken);
            await _helpers.AssignTaskAsync(assignedTaskId, memberId, ownerToken);

            var mentionedTaskId = await _helpers.CreateTaskAsync(projectId, "Read all mentioned task", ownerToken);
            await _helpers.AddTaskCommentAsync(mentionedTaskId, "Ping everyone", ownerToken, new[] { memberId });

            Assert.Equal(2, await _notifications.GetUnreadCountResultAsync(memberToken));

            var response = await _notifications.MarkAllAsReadRequestAsync(memberToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());

            var items = (await _notifications.GetNotificationsResultAsync(memberToken)).Items.ToArray();

            Assert.Equal(2, items.Length);
            Assert.All(items, x => Assert.True(x.IsRead));
            Assert.All(items, x => Assert.NotNull(x.ReadAtUtc));

            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task MarkAllAsRead_LeavesAnotherUsersNotificationsUnread()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("readall-other");
            var (firstId, firstToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "ReadAll First", ownerToken);
            var (secondId, secondToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "ReadAll Second", ownerToken);

            var firstTaskId = await _helpers.CreateTaskAsync(projectId, "First read all task", ownerToken);
            await _helpers.AssignTaskAsync(firstTaskId, firstId, ownerToken);
            var secondTaskId = await _helpers.CreateTaskAsync(projectId, "Second read all task", ownerToken);
            await _helpers.AssignTaskAsync(secondTaskId, secondId, ownerToken);
            var thirdTaskId = await _helpers.CreateTaskAsync(projectId, "Third read all task", ownerToken);
            await _helpers.AssignTaskAsync(thirdTaskId, secondId, ownerToken);

            await _notifications.MarkAllAsReadAsync(firstToken);

            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(firstToken));
            Assert.Equal(2, await _notifications.GetUnreadCountResultAsync(secondToken));
        }

        [Fact]
        public async Task MarkAllAsRead_WithAnEmptyFeed_Returns204AndChangesNothing()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("readall-empty");
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "ReadAll Empty Member", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, (await _notifications.MarkAllAsReadRequestAsync(memberToken)).StatusCode);

            Assert.Empty((await _notifications.GetNotificationsResultAsync(memberToken)).Items);
            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task MarkAllAsRead_KeepsEveryRowInTheFeedAndMovesThemToTheReadFilter()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("readall-rows");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "ReadAll Rows Member", ownerToken);

            for (var i = 1; i <= 3; i++)
            {
                var taskId = await _helpers.CreateTaskAsync(projectId, $"Read all rows task {i}", ownerToken);
                await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            }

            await _notifications.MarkAllAsReadAsync(memberToken);

            var unread = await _notifications.GetNotificationsResultAsync(memberToken, "isRead=false");
            var read = await _notifications.GetNotificationsResultAsync(memberToken, "isRead=true");

            Assert.Empty(unread.Items);
            Assert.Equal(3, read.Items.Count);
            Assert.Equal(3, read.TotalCount);
        }

        [Fact]
        public async Task MarkAllAsRead_ReadsTheNotificationOfADeletedTask()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("readall-deleted");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "ReadAll Deleted Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Read all deleted task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            await _notifications.MarkAllAsReadAsync(memberToken);

            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(memberToken));
            Assert.True(Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items).IsRead);
        }

        [Fact]
        public async Task MarkAllAsRead_SecondCall_Returns204AndKeepsTheFeedUnchanged()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("readall-twice");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "ReadAll Twice Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Read all twice task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            await _notifications.MarkAllAsReadAsync(memberToken);

            var first = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            await _notifications.MarkAllAsReadAsync(memberToken);

            var second = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            Assert.Equal(first.Id, second.Id);
            Assert.Equal(first.ReadAtUtc, second.ReadAtUtc);
        }

        [Fact]
        public async Task MarkAllAsRead_WithoutAToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _notifications.MarkAllAsReadRequestAsync()).StatusCode);
        }
    }
}
