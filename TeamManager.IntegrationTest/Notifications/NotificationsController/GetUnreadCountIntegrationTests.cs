using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Project;
using TeamManager.IntegrationTest.Notifications.Configuration;
using TeamManager.IntegrationTest.Projects.Configuration;

namespace TeamManager.IntegrationTest.Notifications.NotificationsController
{
    [Collection("Projects")]
    public sealed class GetUnreadCountIntegrationTests(ProjectFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly NotificationTestHelpers _notifications = new(factory);

        [Fact]
        public async Task GetUnreadCount_CountsEveryUnreadNotificationOfTheRecipient()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("count-two");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Counted Member", ownerToken);

            for (var i = 1; i <= 2; i++)
            {
                var taskId = await _helpers.CreateTaskAsync(projectId, $"Counted task {i}", ownerToken);
                await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            }

            Assert.Equal(2, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task GetUnreadCount_DropsAsTheRecipientReadsHisNotifications()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("count-read");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Reading Member", ownerToken);

            var firstTaskId = await _helpers.CreateTaskAsync(projectId, "Read counting task", ownerToken);
            await _helpers.AssignTaskAsync(firstTaskId, memberId, ownerToken);
            var secondTaskId = await _helpers.CreateTaskAsync(projectId, "Second read counting task", ownerToken);
            await _helpers.AssignTaskAsync(secondTaskId, memberId, ownerToken);

            var items = (await _notifications.GetNotificationsResultAsync(memberToken)).Items.ToArray();

            await _notifications.MarkAsReadAsync(items[0].Id, memberToken);

            Assert.Equal(1, await _notifications.GetUnreadCountResultAsync(memberToken));

            await _notifications.MarkAsReadAsync(items[1].Id, memberToken);

            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task GetUnreadCount_DoesNotCountTheNotificationsOfAnotherUser()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("count-other");
            var (firstId, firstToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Counted First", ownerToken);
            var (secondId, secondToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Counted Second", ownerToken);

            var firstTaskId = await _helpers.CreateTaskAsync(projectId, "First counted task", ownerToken);
            await _helpers.AssignTaskAsync(firstTaskId, firstId, ownerToken);
            var otherTaskId = await _helpers.CreateTaskAsync(projectId, "Other counted task", ownerToken);
            await _helpers.AssignTaskAsync(otherTaskId, secondId, ownerToken);
            var lastTaskId = await _helpers.CreateTaskAsync(projectId, "Last counted task", ownerToken);
            await _helpers.AssignTaskAsync(lastTaskId, secondId, ownerToken);

            Assert.Equal(1, await _notifications.GetUnreadCountResultAsync(firstToken));
            Assert.Equal(2, await _notifications.GetUnreadCountResultAsync(secondToken));
        }

        [Fact]
        public async Task GetUnreadCount_IsZeroForAUserWithoutNotifications()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("count-none");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Uncounted Member", ownerToken);

            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(memberToken));

            var taskId = await _helpers.CreateTaskAsync(projectId, "Counted after zero task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            Assert.Equal(1, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task GetUnreadCount_ReturnsABareNumberInTheBody()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("count-body");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Body Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Bare number task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            var response = await _notifications.GetUnreadCountAsync(memberToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("1", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task GetUnreadCount_GoesToZeroAfterReadAll()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("count-readall");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "ReadAll Member", ownerToken);

            for (var i = 1; i <= 3; i++)
            {
                var taskId = await _helpers.CreateTaskAsync(projectId, $"Read all task {i}", ownerToken);
                await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            }

            Assert.Equal(3, await _notifications.GetUnreadCountResultAsync(memberToken));

            await _notifications.MarkAllAsReadAsync(memberToken);

            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task GetUnreadCount_IgnoresTheQueryParameters()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("count-query");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Query Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Queried count task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            var response = await _notifications.RequestAsync(HttpMethod.Get, $"{NotificationEndpoints.UnreadCount}?page=7&isRead=true",
                memberToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, await response.Content.ReadFromJsonAsync<int>());
        }

        [Fact]
        public async Task GetUnreadCount_WithoutAToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _notifications.GetUnreadCountAsync()).StatusCode);
        }
    }
}
