using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskChecklist
{
    [Collection("Projects")]
    public sealed class ReopenChecklistItemIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task ReopenChecklistItem_AsProjectOwner_Returns204AndClearsTheCompletion()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-reopen");
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Checklist Reopener", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Reopen task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Reopen me", ownerToken);

            await _helpers.CompleteChecklistItemAsync(taskId, itemId, memberToken);

            var response = await _helpers.ReopenChecklistItemRequestAsync(taskId, itemId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var item = Assert.Single(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken));

            Assert.False(item.IsCompleted);
            Assert.Null(item.CompletedAtUtc);
            Assert.Null(item.CompletedBy);
            Assert.Null(item.CompletedByName);
        }

        [Fact]
        public async Task ReopenChecklistItem_OnADoneTask_Returns204AndLeavesTheTaskDoneWithAnOpenItem()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-reopen-done");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Done reopen task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Closed at completion", ownerToken);

            await _helpers.CompleteChecklistItemAsync(taskId, itemId, ownerToken);
            await _helpers.CompleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.ReopenChecklistItemRequestAsync(taskId, itemId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var item = Assert.Single(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken));

            Assert.False(item.IsCompleted);
            Assert.Null(item.CompletedAtUtc);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(TaskItemStatus.Done, task.Status);
            Assert.NotNull(task.CompletedAtUtc);
        }

        [Fact]
        public async Task ReopenChecklistItem_AsProjectAdminOrMember_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-reopen-roles");
            var (_, adminToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Checklist Admin", ownerToken);
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Checklist Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Roles reopen task", ownerToken);
            var firstItemId = await _helpers.AddChecklistItemAsync(taskId, "Admin reopens", ownerToken);
            var secondItemId = await _helpers.AddChecklistItemAsync(taskId, "Member reopens", ownerToken);

            await _helpers.CompleteChecklistItemAsync(taskId, firstItemId, ownerToken);
            await _helpers.CompleteChecklistItemAsync(taskId, secondItemId, ownerToken);

            var asAdmin = await _helpers.ReopenChecklistItemRequestAsync(taskId, firstItemId, adminToken);
            var asMember = await _helpers.ReopenChecklistItemRequestAsync(taskId, secondItemId, memberToken);

            Assert.Equal(HttpStatusCode.NoContent, asAdmin.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, asMember.StatusCode);

            Assert.All(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken), item => Assert.False(item.IsCompleted));
        }

        [Fact]
        public async Task ReopenChecklistItem_AsProjectViewer_Returns403AndKeepsTheItemCompleted()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-reopen-viewer");
            var (_, viewerToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Checklist Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Viewer reopen task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Viewer cannot reopen", ownerToken);

            await _helpers.CompleteChecklistItemAsync(taskId, itemId, ownerToken);

            var response = await _helpers.ReopenChecklistItemRequestAsync(taskId, itemId, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var item = Assert.Single(await _helpers.GetTaskChecklistResultAsync(taskId, ownerToken));

            Assert.True(item.IsCompleted);
        }

        [Fact]
        public async Task ReopenChecklistItem_AsTeamOwnerWithoutProjectMembership_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-reopen-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("check-reopen-teamowner-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner reopen task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Team owner reopens", ownerToken);

            await _helpers.CompleteChecklistItemAsync(taskId, itemId, ownerToken);

            var response = await _helpers.ReopenChecklistItemRequestAsync(taskId, itemId, teamOwnerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ReopenChecklistItem_ForAnItemOfAnotherTask_Returns400AndKeepsTheOtherItemCompleted()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-reopen-foreign");

            var otherTaskId = await _helpers.CreateTaskAsync(projectId, "Foreign item owner", ownerToken);
            var foreignItemId = await _helpers.AddChecklistItemAsync(otherTaskId, "Belongs to another task", ownerToken);

            await _helpers.CompleteChecklistItemAsync(otherTaskId, foreignItemId, ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Reopen foreign task", ownerToken);

            var response = await _helpers.ReopenChecklistItemRequestAsync(taskId, foreignItemId, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.BadRequest, "Checklist item not found");

            var otherItem = Assert.Single(await _helpers.GetTaskChecklistResultAsync(otherTaskId, ownerToken));

            Assert.True(otherItem.IsCompleted);
        }

        [Fact]
        public async Task ReopenChecklistItem_ForUnknownTask_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-reopen-unknowntask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Unknown task reopen", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Orphan reopen target", ownerToken);

            var response = await _helpers.ReopenChecklistItemRequestAsync(long.MaxValue, itemId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ReopenChecklistItem_AfterTheTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-reopen-deletedtask");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed reopen task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Deleted task item", ownerToken);

            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.ReopenChecklistItemRequestAsync(taskId, itemId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ReopenChecklistItem_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-reopen-notoken");
            var taskId = await _helpers.CreateTaskAsync(projectId, "No token reopen task", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "No token reopen", ownerToken);

            var response = await _helpers.ReopenChecklistItemRequestAsync(taskId, itemId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ReopenChecklistItem_WithZeroTaskId_Returns400WithFieldError()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-reopen-zerotaskid");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Zero task id reopen", ownerToken);
            var itemId = await _helpers.AddChecklistItemAsync(taskId, "Zero task id reopen item", ownerToken);

            var response = await _helpers.ReopenChecklistItemRequestAsync(0, itemId, ownerToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "TaskId");
        }

        [Fact]
        public async Task ReopenChecklistItem_WithZeroChecklistItemId_Returns400WithFieldError()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("check-reopen-zeroitemid");
            var taskId = await _helpers.CreateTaskAsync(projectId, "Zero item id reopen", ownerToken);

            var response = await _helpers.ReopenChecklistItemRequestAsync(taskId, 0, ownerToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "ChecklistItemId");
        }
    }
}
