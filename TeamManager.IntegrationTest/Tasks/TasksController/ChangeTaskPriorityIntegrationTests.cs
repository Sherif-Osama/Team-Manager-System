using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Task;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TasksController
{
    [Collection("Tasks")]
    public sealed class ChangeTaskPriorityIntegrationTests(TaskItemFactory factory) : IntegrationTestBase(factory)
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
        public async Task ChangeTaskPriority_AsProjectOwner_StoresTheNewPriority()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("taskpriority");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Priority task", ownerToken);

            var response = await _helpers.ChangeTaskPriorityRequestAsync(taskId, new { priority = TaskPriority.Urgent }, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(TaskPriority.Urgent, task.Priority);
        }

        [Fact]
        public async Task ChangeTaskPriority_AsProjectAdmin_StoresTheNewPriority()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("taskpriority-admin");
            var (_, adminToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Admin, "Priority Admin", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Admin priority task", ownerToken);

            var response = await _helpers.ChangeTaskPriorityRequestAsync(taskId, new { priority = TaskPriority.Low }, adminToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskPriority_AsProjectMemberOrViewer_Returns403()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("taskpriority-roles");
            var (_, memberToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Priority Member", ownerToken);
            var (_, viewerToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Viewer, "Priority Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Protected priority task", ownerToken);

            var asMember = await _helpers.ChangeTaskPriorityRequestAsync(taskId, new { priority = TaskPriority.High }, memberToken);
            var asViewer = await _helpers.ChangeTaskPriorityRequestAsync(taskId, new { priority = TaskPriority.High }, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asViewer.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskPriority_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("taskpriority-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("taskpriority-outsider-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Private priority task", ownerToken);

            var response = await _helpers.ChangeTaskPriorityRequestAsync(taskId, new { priority = TaskPriority.High }, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskPriority_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("taskpriority-notoken");

            var taskId = await _helpers.CreateTaskAsync(projectId, "No token priority task", ownerToken);

            var response = await _helpers.ChangeTaskPriorityRequestAsync(taskId, new { priority = TaskPriority.High });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskPriority_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("taskpriority-unknown");

            var response = await _helpers.ChangeTaskPriorityRequestAsync(long.MaxValue, new { priority = TaskPriority.High }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskPriority_AfterTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("taskpriority-deletedtask");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed priority task", ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.ChangeTaskPriorityRequestAsync(taskId, new { priority = TaskPriority.High }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


    }
}
