using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Project;
using TeamManager.IntegrationTest.Notifications.Configuration;
using TeamManager.IntegrationTest.Projects.Configuration;

namespace TeamManager.IntegrationTest.Notifications.NotificationsController
{
    [Collection("Projects")]
    public sealed class GetMyNotificationsIntegrationTests(ProjectFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly NotificationTestHelpers _notifications = new(factory);

        [Fact]
        public async Task GetMyNotifications_AsTheRecipient_ReturnsTheItemShapeAndTheEnvelopeDefaults()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("list-shape");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Listed Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Listed task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            var result = await _notifications.GetNotificationsResultAsync(memberToken);

            var item = Assert.Single(result.Items);

            Assert.True(item.Id > 0);
            Assert.Equal(NotificationType.TaskAssigned, item.Type);
            Assert.Equal("Task Assigned", item.Title);
            Assert.Equal("You have been assigned to a task.", item.Body);
            Assert.Equal("Task", item.RelatedEntityType);
            Assert.Equal(taskId.ToString(), item.RelatedEntityId);
            Assert.False(item.IsRead);
            Assert.Null(item.ReadAtUtc);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);
        }

        [Fact]
        public async Task GetMyNotifications_DoesNotListTheNotificationsOfAnotherRecipient()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("list-isolation");
            var (firstId, firstToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "First Recipient", ownerToken);
            var (secondId, secondToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Second Recipient", ownerToken);

            var firstTaskId = await _helpers.CreateTaskAsync(projectId, "First task", ownerToken);
            await _helpers.AssignTaskAsync(firstTaskId, firstId, ownerToken);

            var secondTaskId = await _helpers.CreateTaskAsync(projectId, "Second task", ownerToken);
            await _helpers.AssignTaskAsync(secondTaskId, secondId, ownerToken);

            var first = Assert.Single((await _notifications.GetNotificationsResultAsync(firstToken)).Items);
            var second = Assert.Single((await _notifications.GetNotificationsResultAsync(secondToken)).Items);

            Assert.Equal(firstTaskId.ToString(), first.RelatedEntityId);
            Assert.Equal(secondTaskId.ToString(), second.RelatedEntityId);
        }

        [Fact]
        public async Task GetMyNotifications_WithIsReadTrue_ReturnsOnlyTheReadOnes()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("list-isread-true");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Read Filter Member", ownerToken);

            var readTaskId = await _helpers.CreateTaskAsync(projectId, "Read state task", ownerToken);
            await _helpers.AssignTaskAsync(readTaskId, memberId, ownerToken);
            var unreadTaskId = await _helpers.CreateTaskAsync(projectId, "Unread state task", ownerToken);
            await _helpers.AssignTaskAsync(unreadTaskId, memberId, ownerToken);

            var items = (await _notifications.GetNotificationsResultAsync(memberToken)).Items.ToArray();
            await _notifications.MarkAsReadAsync(items[0].Id, memberToken);

            var result = await _notifications.GetNotificationsResultAsync(memberToken, "isRead=true");

            var item = Assert.Single(result.Items);

            Assert.Equal(items[0].Id, item.Id);
            Assert.True(item.IsRead);
            Assert.Equal(1, result.TotalCount);

            Assert.Equal(readTaskId.ToString(), (await _notifications.GetNotificationsResultAsync(memberToken, "isRead=false"))
                .Items.Single().RelatedEntityId);
        }

        [Fact]
        public async Task GetMyNotifications_OrdersTheFeedNewestFirst()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("list-order");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Ordered Member", ownerToken);

            foreach (var title in new[] { "Order first task", "Order second task", "Order third task" })
            {
                var taskId = await _helpers.CreateTaskAsync(projectId, title, ownerToken);
                await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            }

            var created = (await _notifications.GetNotificationsResultAsync(memberToken)).Items
                .Select(x => x.CreatedAtUtc).ToArray();

            Assert.Equal(created.OrderByDescending(x => x).ToArray(), created);
        }

        [Fact]
        public async Task GetMyNotifications_PagesTheFeedWithoutRepeatingItems()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("list-page");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Paged Member", ownerToken);

            for (var i = 1; i <= 3; i++)
            {
                var taskId = await _helpers.CreateTaskAsync(projectId, $"Paged task {i}", ownerToken);
                await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            }

            var firstPage = await _notifications.GetNotificationsResultAsync(memberToken, "page=1&pageSize=2");
            var secondPage = await _notifications.GetNotificationsResultAsync(memberToken, "page=2&pageSize=2");

            Assert.Equal(2, firstPage.Items.Count);
            Assert.Single(secondPage.Items);

            Assert.Equal(3, firstPage.TotalCount);
            Assert.Equal(3, secondPage.TotalCount);
            Assert.Equal(2, firstPage.PageSize);
            Assert.Equal(2, secondPage.Page);

            Assert.Empty(firstPage.Items.Select(x => x.Id).Intersect(secondPage.Items.Select(x => x.Id)));
        }

        [Fact]
        public async Task GetMyNotifications_APageBeyondTheEnd_ReturnsNoItemsAndKeepsTheTotalCount()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("list-beyond");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Beyond Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Beyond end task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            var result = await _notifications.GetNotificationsResultAsync(memberToken, "page=4&pageSize=10");

            Assert.Empty(result.Items);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(4, result.Page);
        }

        [Fact]
        public async Task GetMyNotifications_ForAUserWithoutNotifications_ReturnsAnEmptyFeed()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("list-empty");
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Silent Member", ownerToken);

            var result = await _notifications.GetNotificationsResultAsync(memberToken);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
            Assert.Equal(0, await _notifications.GetUnreadCountResultAsync(memberToken));
        }

        [Fact]
        public async Task GetMyNotifications_ShowsaNewNotificationInTheNextRequestWithoutAnyEviction()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("list-fresh");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Fresh Member", ownerToken);

            var firstTaskId = await _helpers.CreateTaskAsync(projectId, "Fresh first task", ownerToken);
            await _helpers.AssignTaskAsync(firstTaskId, memberId, ownerToken);

            Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            var secondTaskId = await _helpers.CreateTaskAsync(projectId, "Fresh second task", ownerToken);
            await _helpers.AssignTaskAsync(secondTaskId, memberId, ownerToken);

            var related = (await _notifications.GetNotificationsResultAsync(memberToken)).Items
                .Select(x => x.RelatedEntityId).ToArray();

            Assert.Equal(2, related.Length);
            Assert.Contains(firstTaskId.ToString(), related);
            Assert.Contains(secondTaskId.ToString(), related);
        }

        [Fact]
        public async Task GetMyNotifications_StillListsTheNotificationAfterTheTaskIsDeleted()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("list-deletedtask");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Deleted Task Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Deleted link task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            Assert.Equal(taskId.ToString(), item.RelatedEntityId);
            Assert.Equal(HttpStatusCode.NotFound, (await _helpers.GetTaskAsync(taskId, memberToken)).StatusCode);
        }

        [Fact]
        public async Task GetMyNotifications_StillListsTheNotificationAfterTheProjectIsDeleted()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("list-deletedproject");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Deleted Project Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Deleted project task", ownerToken);
            await _helpers.AssignTaskAsync(taskId, memberId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, (await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.Cancelled, ownerToken))
                .StatusCode);
            await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);

            var item = Assert.Single((await _notifications.GetNotificationsResultAsync(memberToken)).Items);

            Assert.Equal(taskId.ToString(), item.RelatedEntityId);
        }




        [Fact]
        public async Task GetMyNotifications_WithoutAToken_Returns401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _notifications.GetNotificationsAsync()).StatusCode);
        }
    }
}
