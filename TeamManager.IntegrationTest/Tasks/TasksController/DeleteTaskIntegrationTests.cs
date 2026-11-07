using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TasksController
{
    [Collection("Common")]
    public sealed class DeleteTaskIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
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
        public async Task DeleteTask_AsProjectOwner_SoftDeletesTheTask()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("deletetask");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Deletable task", ownerToken);

            var response = await _helpers.DeleteTaskRequestAsync(taskId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var get = await _helpers.GetTaskAsync(taskId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact]
        public async Task DeleteTask_AsProjectAdmin_SoftDeletesTheTask()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("deletetask-admin");
            var (_, adminToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Admin, "Delete Admin", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Admin deletable task", ownerToken);

            var response = await _helpers.DeleteTaskRequestAsync(taskId, adminToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTask_AsProjectMemberOrViewer_Returns403()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("deletetask-roles");
            var (_, memberToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Delete Member", ownerToken);
            var (_, viewerToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Viewer, "Delete Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Protected deletable task", ownerToken);

            var asMember = await _helpers.DeleteTaskRequestAsync(taskId, memberToken);
            var asViewer = await _helpers.DeleteTaskRequestAsync(taskId, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asViewer.StatusCode);
        }

        [Fact]
        public async Task DeleteTask_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("deletetask-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("deletetask-outsider-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Private deletable task", ownerToken);

            var response = await _helpers.DeleteTaskRequestAsync(taskId, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTask_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("deletetask-notoken");

            var taskId = await _helpers.CreateTaskAsync(projectId, "No token deletable task", ownerToken);

            var response = await _helpers.DeleteTaskRequestAsync(taskId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTask_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("deletetask-unknown");

            var response = await _helpers.DeleteTaskRequestAsync(long.MaxValue, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTask_AfterTaskIsAlreadyDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("deletetask-twice");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed deletable task", ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.DeleteTaskRequestAsync(taskId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


        [Fact]
        public async Task DeleteTask_DeletedTaskDoesNotBlockProjectDeletion()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("deletetask-project");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Only task", ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Fact]
        public async Task DeleteTask_RemovesTheDependencyRowsInBothDirections()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("deletetask-dependency");

            var predecessorId = await _helpers.CreateTaskAsync(projectId, "Predecessor", ownerToken);
            var dependentId = await _helpers.CreateTaskAsync(projectId, "Dependent", ownerToken);

            await _helpers.AddTaskDependencyAsync(dependentId, predecessorId, ownerToken);

            await _helpers.DeleteTaskAsync(predecessorId, ownerToken);

            var listed = await _helpers.GetTaskDependenciesResultAsync(dependentId, ownerToken);

            Assert.Empty(listed.Items);

            await _helpers.CompleteTaskAsync(dependentId, ownerToken);
        }
    }
}
