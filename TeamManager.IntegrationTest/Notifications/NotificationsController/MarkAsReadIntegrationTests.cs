using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Notifications.Configuration;
using TeamManager.IntegrationTest.Projects.Configuration;

namespace TeamManager.IntegrationTest.Notifications.NotificationsController
{
    [Collection("Projects")]
    public sealed class MarkAsReadIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly NotificationTestHelpers _notifications = new(factory);

        [Fact]
        public async Task MarkAsRead_AsTheRecipient_Returns204AndFlagsTheNotificationRead()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("read-one");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Reading Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Marked task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            var response = await _notifications.MarkAsReadRequestAsync(item.Id, memberToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());

            var read = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            Assert.Equal(item.Id, read.Id);
            Assert.True(read.IsRead);
            Assert.NotNull(read.ReadAtUtc);

            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task MarkAsRead_LeavesTheOtherNotificationsOfTheSameRecipientUnread()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("read-otherstays");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Two Items Member", ownerToken);

            var firstTaskId = await _helpers.CreateTaskAsync(projectId, "First marked task", ownerToken);
            await _helpers.AssignTaskAsync(firstTaskId, memberId, ownerToken);
            var secondTaskId = await _helpers.CreateTaskAsync(projectId, "Second marked task", ownerToken);
            await _helpers.AssignTaskAsync(secondTaskId, memberId, ownerToken);

            var items = (await _notifications.GetNotificationsResultAsync(memberToken)).Items.ToArray();
            await _notifications.MarkAsReadAsync(items[0].Id, memberToken);

            var after = (await _notifications.GetNotificationsResultAsync(memberToken)).Items.ToArray();

            Assert.NotNull(after.Single(x => x.Id == items[0].Id).ReadAtUtc);
            Assert.False(after.Single(x => x.Id == items[1].Id).IsRead);
            Assert.Null(after.Single(x => x.Id == items[1].Id).ReadAtUtc);

            Assert.Equal(1, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task MarkAsRead_AgainstAnotherUsersNotification_Returns404AndLeavesItUnread()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("read-foreign");
            var (victimId, victimToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Foreign Reader Victim", ownerToken);
            var (_, intruderToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Foreign Reader Intruder", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Foreign read task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, victimId, ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(victimToken)).Items);

            await ProblemDetailsAssertions.AssertDetailAsync(await _notifications.MarkAsReadRequestAsync(item.Id, intruderToken),
                HttpStatusCode.NotFound, "was not found");

            var after = Assert.Single((await _notifications.GetNotificationsResultAsync(victimToken)).Items);

            Assert.False(after.IsRead);
            Assert.Null(after.ReadAtUtc);
            Assert.Equal(1, await _notifications.GetUnreadCountResultAsync(victimToken));
        }

        [Fact]
        public async Task MarkAsRead_AsTheProjectOwner_Returns404ForAMemberNotification()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("read-owner");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Owner Read Victim", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Owner read task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            await ProblemDetailsAssertions.AssertDetailAsync(await _notifications.MarkAsReadRequestAsync(item.Id, ownerToken),
                HttpStatusCode.NotFound, "was not found");
        }

        [Fact]
        public async Task MarkAsRead_WithAnUnknownId_Returns404()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("read-unknown");

            await ProblemDetailsAssertions.AssertDetailAsync(await _notifications.MarkAsReadRequestAsync(987654321, ownerToken),
                HttpStatusCode.NotFound, "was not found");
        }

        [Fact]
        public async Task MarkAsRead_WithZeroId_Returns400FieldError()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("read-zero");

            await ValidationAssertions.AssertFieldErrorAsync(await _notifications.MarkAsReadRequestAsync(0, ownerToken),
                "NotificationId");
        }

        [Fact]
        public async Task MarkAsRead_WithANegativeId_Returns400FieldError()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("read-negative");

            await ValidationAssertions.AssertFieldErrorAsync(
                await _notifications.RequestAsync(HttpMethod.Put, "/api/notifications/-4/read", ownerToken), "NotificationId");
        }

        [Fact]
        public async Task MarkAsRead_WithANonNumericId_Returns404FromTheRouteConstraint()
        {
            var (_, _, ownerToken) = await _helpers.CreateOwnedProjectAsync("read-notnumber");

            var response = await _notifications.RequestAsync(HttpMethod.Put, "/api/notifications/notanumber/read", ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task MarkAsRead_WithoutAToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _notifications.MarkAsReadRequestAsync(1)).StatusCode);
        }

        [Fact]
        public async Task MarkAsRead_OnAnAlreadyReadNotification_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("read-twice");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Twice Reader", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Double read task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            await _notifications.MarkAsReadAsync(item.Id, memberToken);

            Assert.Equal(HttpStatusCode.NoContent, (await _notifications.MarkAsReadRequestAsync(item.Id, memberToken)).StatusCode);
            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task MarkAsRead_OnADeletedTasksNotification_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("read-deletedtask");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Deleted Task Reader", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Deleted before read task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            await _notifications.MarkAsReadAsync(item.Id, memberToken);

            Assert.True(Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items).IsRead);
        }

        [Fact]
        public async Task MarkAsRead_AfterTheRecipientLeavesTheProject_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("read-removed");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Removed Reader", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Task before leaving", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            await _helpers.RemoveProjectMemberAsync(projectId, await _helpers.GetProjectMemberIdAsync(projectId, memberId), ownerToken);

            await _notifications.MarkAsReadAsync(item.Id, memberToken);

            Assert.True(Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items).IsRead);
        }
    }
}
