using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Task;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TasksController
{
    [Collection("Tasks")]
    public sealed class UpdateTaskIntegrationTests(TaskItemFactory factory) : IntegrationTestBase(factory)
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
        public async Task UpdateTask_AsProjectOwner_UpdatesTitleAndDescription()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("updatetask");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Old title", ownerToken);

            var response = await _helpers.UpdateTaskRequestAsync(taskId,
                new { title = "New title", description = "New description" }, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal("New title", task.Title);
            Assert.Equal("New description", task.Description);
        }

        [Fact]
        public async Task UpdateTask_WithoutDescription_StoresNull()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("updatetask-nulldescription");

            var created = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Described task", description = "Old description", priority = TaskPriority.Medium }, ownerToken);

            var taskId = await created.Content.ReadFromJsonAsync<long>();

            var response = await _helpers.UpdateTaskRequestAsync(taskId, new { title = "Described task" }, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Null(task.Description);
        }

        [Fact]
        public async Task UpdateTask_AsProjectAdminOrMemberOrViewer_Returns403()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("updatetask-roles");
            var (_, adminToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Admin, "Update Admin", ownerToken);
            var (_, memberToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Update Member", ownerToken);
            var (_, viewerToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Viewer, "Update Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Protected task", ownerToken);

            var asAdmin = await _helpers.UpdateTaskRequestAsync(taskId, new { title = "Admin edit" }, adminToken);
            var asMember = await _helpers.UpdateTaskRequestAsync(taskId, new { title = "Member edit" }, memberToken);
            var asViewer = await _helpers.UpdateTaskRequestAsync(taskId, new { title = "Viewer edit" }, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asAdmin.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asViewer.StatusCode);
        }

        [Fact]
        public async Task UpdateTask_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("updatetask-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("updatetask-outsider-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Private task", ownerToken);

            var response = await _helpers.UpdateTaskRequestAsync(taskId, new { title = "Stranger edit" }, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTask_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("updatetask-notoken");

            var taskId = await _helpers.CreateTaskAsync(projectId, "No token task", ownerToken);

            var response = await _helpers.UpdateTaskRequestAsync(taskId, new { title = "No token edit" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTask_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("updatetask-unknown");

            var response = await _helpers.UpdateTaskRequestAsync(long.MaxValue, new { title = "Ghost edit" }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTask_AfterTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("updatetask-deletedtask");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed task", ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.UpdateTaskRequestAsync(taskId, new { title = "Zombie edit" }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }



    }
}
