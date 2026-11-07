using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TasksController
{
    [Collection("Common")]
    public sealed class GetTaskIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<(Guid TeamId, Guid ProjectId, Guid OwnerId, string OwnerToken)> CreateOwnedProjectAsync(string prefix)
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync($"{prefix}-owner", $"{prefix} Owner");
            var (teamId, projectId) = await _helpers.CreateProjectAsync($"Tasks {Guid.NewGuid():N}", ownerToken);

            return (teamId, projectId, ownerId, ownerToken);
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
        public async Task GetTask_AsProjectOwner_ReturnsFullShapeWithNamesAndCounts()
        {
            var (teamId, projectId, ownerId, ownerToken) = await CreateOwnedProjectAsync("gettask");
            var (assigneeId, _) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Shape Assignee", ownerToken);

            var project = await _helpers.GetProjectResultAsync(projectId, ownerToken);

            var created = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Shape task", assigneeUserId = assigneeId, priority = TaskPriority.Urgent }, ownerToken);

            var taskId = await created.Content.ReadFromJsonAsync<long>();
            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(taskId, task.Id);
            Assert.Equal(projectId, task.ProjectId);
            Assert.Equal(project.ProjectName, task.ProjectName);
            Assert.Equal("Shape task", task.Title);
            Assert.Equal(ownerId, task.CreatedBy);
            Assert.Equal("gettask Owner", task.CreatorName);
            Assert.Equal(assigneeId, task.AssigneeUserId);
            Assert.Equal("Shape Assignee", task.AssigneeName);
            Assert.Equal(TaskItemStatus.Todo, task.Status);
            Assert.Equal(TaskPriority.Urgent, task.Priority);
            Assert.Null(task.CompletedAtUtc);
            // Assigning during creation touches the task, so a fresh assigned task is already "updated".
            Assert.NotNull(task.UpdatedAtUtc);
            Assert.True(task.CreatedAtUtc > DateTime.UtcNow.AddMinutes(-1));
            Assert.Equal(0, task.DependenciesCount);
            Assert.Equal(0, task.LabelsCount);
            Assert.Equal(0, task.ChecklistItemsCount);
            Assert.Equal(0, task.AttachmentsCount);
            Assert.Equal(0, task.CommentsCount);
        }

        [Fact]
        public async Task GetTask_AsProjectMemberOrViewer_Returns200()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("gettask-member");
            var (_, memberToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Get Member", ownerToken);
            var (_, viewerToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Viewer, "Get Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Shared task", ownerToken);

            var asMember = await _helpers.GetTaskAsync(taskId, memberToken);
            var asViewer = await _helpers.GetTaskAsync(taskId, viewerToken);

            Assert.Equal(HttpStatusCode.OK, asMember.StatusCode);
            Assert.Equal(HttpStatusCode.OK, asViewer.StatusCode);
        }

        [Fact]
        public async Task GetTask_AsTeamOwnerWithoutProjectMembership_Returns200()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("gettask-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("gettask-teamowner");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });
            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner task", ownerToken);

            var response = await _helpers.GetTaskAsync(taskId, teamOwnerToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetTask_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, _, ownerToken) = await CreateOwnedProjectAsync("gettask-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("gettask-outsider-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Private task", ownerToken);

            var response = await _helpers.GetTaskAsync(taskId, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTask_WithoutAccessToken_Returns401()
        {
            var (_, projectId, _, ownerToken) = await CreateOwnedProjectAsync("gettask-notoken");

            var taskId = await _helpers.CreateTaskAsync(projectId, "No token task", ownerToken);

            var response = await _helpers.GetTaskAsync(taskId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTask_ForUnknownTask_Returns404()
        {
            var (_, projectId, _, ownerToken) = await CreateOwnedProjectAsync("gettask-unknown");

            await _helpers.CreateTaskAsync(projectId, "Existing task", ownerToken);

            var response = await _helpers.GetTaskAsync(long.MaxValue, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTask_AfterTaskIsDeleted_Returns404()
        {
            var (_, projectId, _, ownerToken) = await CreateOwnedProjectAsync("gettask-deletedtask");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed task", ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.GetTaskAsync(taskId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTask_AfterProjectIsDeleted_Returns404()
        {
            var (_, projectId, _, ownerToken) = await CreateOwnedProjectAsync("gettask-deletedproject");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Orphan task", ownerToken);

            var cancel = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.Cancelled, ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var response = await _helpers.GetTaskAsync(taskId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

    }
}
