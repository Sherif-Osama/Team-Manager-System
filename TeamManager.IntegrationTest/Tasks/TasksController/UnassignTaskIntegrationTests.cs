using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TasksController
{
    [Collection("Projects")]
    public sealed class UnassignTaskIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<(Guid TeamId, Guid ProjectId, string OwnerToken)> CreateOwnedProjectAsync(string prefix)
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync($"{prefix}-owner");
            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Tasks {Guid.NewGuid():N}", ownerToken);

            return (teamId, projectId, ownerToken);
        }

        private async Task<(Guid UserId, string Token)> AddMemberAsync(Guid teamId, Guid projectId, ProjectRole role,
            string label, string ownerToken)
        {
            var (userId, _, token) = await _authenticatedUser.CreateConfirmedUserAsync(label.Replace(' ', '-'), label);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, userId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            await _helpers.AddProjectMemberAsync(projectId, userId, role, ownerToken);

            return (userId, token);
        }

        [Fact]
        public async Task UnassignTask_AsProjectOwner_ClearsTheAssigneeAndKeepsTheStatus()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("unassign");
            var (memberId, _) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Unassign Target", ownerToken);

            var created = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Unassignable task", assigneeUserId = memberId, priority = TaskPriority.Medium }, ownerToken);

            var taskId = await created.Content.ReadFromJsonAsync<long>();

            var toInProgress = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.InProgress, ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, toInProgress.StatusCode);

            var response = await _helpers.UnassignTaskRequestAsync(taskId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Null(task.AssigneeUserId);
            Assert.Null(task.AssigneeName);
            Assert.Equal(TaskItemStatus.InProgress, task.Status);
        }

        [Fact]
        public async Task UnassignTask_AsProjectAdmin_ClearsTheAssignee()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("unassign-admin");
            var (_, adminToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Admin, "Unassign Admin", ownerToken);
            var (memberId, _) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Admin Target", ownerToken);

            var created = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Admin unassignable task", assigneeUserId = memberId, priority = TaskPriority.Medium }, ownerToken);

            var taskId = await created.Content.ReadFromJsonAsync<long>();

            var response = await _helpers.UnassignTaskRequestAsync(taskId, adminToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task UnassignTask_AsProjectMemberOrViewer_Returns403()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("unassign-roles");
            var (_, memberToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Unassign Member", ownerToken);
            var (targetId, _) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Unassign Target", ownerToken);
            var (_, viewerToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Viewer, "Unassign Viewer", ownerToken);

            var created = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Protected task", assigneeUserId = targetId, priority = TaskPriority.Medium }, ownerToken);

            var taskId = await created.Content.ReadFromJsonAsync<long>();

            var asMember = await _helpers.UnassignTaskRequestAsync(taskId, memberToken);
            var asViewer = await _helpers.UnassignTaskRequestAsync(taskId, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asViewer.StatusCode);
        }

        [Fact]
        public async Task UnassignTask_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("unassign-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("unassign-outsider-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Private task", ownerToken);

            var response = await _helpers.UnassignTaskRequestAsync(taskId, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UnassignTask_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("unassign-notoken");

            var taskId = await _helpers.CreateTaskAsync(projectId, "No token task", ownerToken);

            var response = await _helpers.UnassignTaskRequestAsync(taskId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UnassignTask_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("unassign-unknown");

            var response = await _helpers.UnassignTaskRequestAsync(long.MaxValue, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UnassignTask_AfterTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("unassign-deletedtask");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed task", ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.UnassignTaskRequestAsync(taskId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

    }
}
