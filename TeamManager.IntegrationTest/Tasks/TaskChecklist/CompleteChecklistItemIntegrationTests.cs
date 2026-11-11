using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Task;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskChecklist
{
    [Collection("Tasks")]
    public sealed class CompleteChecklistItemIntegrationTests(TaskItemFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task CompleteChecklistItem_AsProjectMember_MarksTheItemAndRecordsWhoCompletedIt()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-complete");
            var (memberId, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Checklist Completer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Complete task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Ship it", ownerToken);

            var response = await _helpers.CompleteChecklistItemRequestAsync(taskId, itemId, memberToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var items = await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken);

            var item = Assert.Single(items);

            Assert.True(item.IsCompleted);
            Assert.NotNull(item.CompletedAtUtc);
            Assert.Equal(memberId, item.CompletedBy);
            Assert.Equal("Checklist Completer", item.CompletedByName);
        }

        [Fact]
        public async Task CompleteChecklistItem_AsProjectAdmin_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-complete-admin");
            var (_, adminToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Checklist Admin", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Admin complete task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Admin completes", ownerToken);

            var response = await _helpers.CompleteChecklistItemRequestAsync(taskId, itemId, adminToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task CompleteChecklistItem_AsProjectViewer_Returns403AndKeepsTheItemOpen()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-complete-viewer");
            var (_, viewerToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Checklist Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Viewer complete task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Viewer cannot complete", ownerToken);

            var response = await _helpers.CompleteChecklistItemRequestAsync(taskId, itemId, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var item = Assert.Single(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken));

            Assert.False(item.IsCompleted);
            Assert.Null(item.CompletedBy);
        }

        [Fact]
        public async Task CompleteChecklistItem_AsTeamOwnerWithoutProjectMembership_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-complete-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("check-complete-teamowner-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner complete task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Team owner completes", ownerToken);

            var response = await _helpers.CompleteChecklistItemRequestAsync(taskId, itemId, teamOwnerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task CompleteChecklistItem_ForAnItemOfAnotherTask_Returns400AndLeavesTheOtherItemOpen()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-complete-foreign");

            var otherTaskId = await _helpers.CreateTaskAsync(projectId, "Foreign item owner", ownerToken);
            var foreignItemId = await _helpers.AddChecklistItemAsync(otherTaskId, "Belongs to another task", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Complete foreign task", ownerToken);

            var response = await _helpers.CompleteChecklistItemRequestAsync(taskId, foreignItemId, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.BadRequest, "Checklist item not found");

            var otherItem = Assert.Single(await _helpers.GetTaskChecklistResultAsync(otherTaskId, ownerToken));

            Assert.False(otherItem.IsCompleted);
        }

        [Fact]
        public async Task CompleteChecklistItem_ForUnknownTask_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-complete-unknowntask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Unknown task complete", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Orphan target", ownerToken);

            var response = await _helpers.CompleteChecklistItemRequestAsync(long.MaxValue, itemId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CompleteChecklistItem_AfterTheTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-complete-deletedtask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed complete task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Deleted task item", ownerToken);

            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.CompleteChecklistItemRequestAsync(taskId, itemId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CompleteChecklistItem_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-complete-notoken");
            var taskId = await _helpers.CreateTaskAsync(projectId, "No token complete task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "No token item", ownerToken);

            var response = await _helpers.CompleteChecklistItemRequestAsync(taskId, itemId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }


    }
}
