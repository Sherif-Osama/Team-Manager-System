using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TasksController
{
    [Collection("Projects")]
    public sealed class CreateTaskIntegrationTests(ProjectsFixture factory)
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
        public async Task CreateTask_AsProjectOwner_WithAllFields_Returns201AndStoresTheTask()
        {
            var (teamId, projectId, ownerId, ownerToken) = await CreateOwnedProjectAsync("createtask");
            var (assigneeId, _) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Task Assignee", ownerToken);

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var dueDate = startDate.AddDays(5);

            var response = await _helpers.CreateTaskRequestAsync(projectId, new
            {
                title = "Design the homepage",
                description = "Cover desktop and mobile layouts",
                assigneeUserId = assigneeId,
                startDate,
                dueDate,
                priority = TaskPriority.High
            }, ownerToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var taskId = await response.Content.ReadFromJsonAsync<long>();
            Assert.Equal($"/api/Tasks/{taskId}", response.Headers.Location!.AbsolutePath);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal("Design the homepage", task.Title);
            Assert.Equal("Cover desktop and mobile layouts", task.Description);
            Assert.Equal(ownerId, task.CreatedBy);
            Assert.Equal(assigneeId, task.AssigneeUserId);
            Assert.Equal(startDate, task.StartDate);
            Assert.Equal(dueDate, task.DueDate);
            Assert.Equal(TaskPriority.High, task.Priority);
            Assert.Equal(TaskItemStatus.Todo, task.Status);
        }

        [Fact]
        public async Task CreateTask_WithMinimumBody_LeavesOptionalFieldsNull()
        {
            var (_, projectId, _, ownerToken) = await CreateOwnedProjectAsync("createtask-minimal");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Minimal task", ownerToken);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal("Minimal task", task.Title);
            Assert.Equal(TaskPriority.Medium, task.Priority);
            Assert.Null(task.Description);
            Assert.Null(task.AssigneeUserId);
            Assert.Null(task.StartDate);
            Assert.Null(task.DueDate);
            Assert.Null(task.UpdatedAtUtc);
        }

        [Fact]
        public async Task CreateTask_AsProjectAdmin_Returns201()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("createtask-admin");
            var (_, adminToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Admin, "Task Admin", ownerToken);

            var response = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Admin task", priority = TaskPriority.Low }, adminToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateTask_AsProjectMemberOrViewer_Returns403()
        {
            var (teamId, projectId, _, ownerToken) = await CreateOwnedProjectAsync("createtask-member");
            var (_, memberToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Task Member", ownerToken);
            var (_, viewerToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Viewer, "Task Viewer", ownerToken);

            var asMember = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Member task", priority = TaskPriority.Medium }, memberToken);
            var asViewer = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Viewer task", priority = TaskPriority.Medium }, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asViewer.StatusCode);
        }

        [Fact]
        public async Task CreateTask_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, _, ownerToken) = await CreateOwnedProjectAsync("createtask-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("createtask-outsider-stranger");

            var response = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Stranger task", priority = TaskPriority.Medium }, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateTask_WithoutAccessToken_Returns401()
        {
            var (_, projectId, _, _) = await CreateOwnedProjectAsync("createtask-notoken");

            var response = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "No token task", priority = TaskPriority.Medium });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateTask_ForUnknownProject_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("createtask-unknownproject");

            var response = await _helpers.CreateTaskRequestAsync(Guid.NewGuid(),
                new { title = "Ghost project task", priority = TaskPriority.Medium }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateTask_AfterProjectIsDeleted_Returns404()
        {
            var (_, projectId, _, ownerToken) = await CreateOwnedProjectAsync("createtask-deletedproject");

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var response = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Doomed task", priority = TaskPriority.Medium }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateTask_OnNonActiveProject_Returns400()
        {
            var (_, projectId, _, ownerToken) = await CreateOwnedProjectAsync("createtask-hold");

            var status = await _helpers.ChangeProjectStatusAsync(projectId, ProjectStatus.OnHold, ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, status.StatusCode);

            var response = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "On hold task", priority = TaskPriority.Medium }, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }




        [Fact]
        public async Task CreateTask_ForAssigneeWhoIsNotAProjectMember_Returns404()
        {
            var (_, projectId, _, ownerToken) = await CreateOwnedProjectAsync("createtask-foreignassignee");
            var (strangerId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("createtask-foreignassignee-user");

            var response = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Foreign assignee task", assigneeUserId = strangerId, priority = TaskPriority.Medium }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

    }
}
