using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TasksController
{
    [Collection("Projects")]
    public sealed class AssignTaskIntegrationTests(ProjectsFixture factory)
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
        public async Task AssignTask_AsProjectOwner_AssignsAnActiveMember()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("assign");
            var (memberId, _) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Assign Target", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Assignable task", ownerToken);

            var response = await _helpers.AssignTaskRequestAsync(taskId, new { userId = memberId }, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(memberId, task.AssigneeUserId);
            Assert.Equal("Assign Target", task.AssigneeName);
        }

        [Fact]
        public async Task AssignTask_AsProjectAdmin_AssignsAnActiveMember()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("assign-admin");
            var (_, adminToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Admin, "Assign Admin", ownerToken);
            var (memberId, _) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Admin Target", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Admin assignable task", ownerToken);

            var response = await _helpers.AssignTaskRequestAsync(taskId, new { userId = memberId }, adminToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task AssignTask_ReassigningToAnotherMember_MovesTheTask()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("assign-reassign");
            var (firstId, _) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "First Assignee", ownerToken);
            var (secondId, _) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Second Assignee", ownerToken);

            var created = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Reassignable task", assigneeUserId = firstId, priority = TaskPriority.Medium }, ownerToken);

            var taskId = await created.Content.ReadFromJsonAsync<long>();

            await _helpers.AssignTaskAsync(taskId, secondId, ownerToken);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(secondId, task.AssigneeUserId);
            Assert.Equal("Second Assignee", task.AssigneeName);
        }

        [Fact]
        public async Task AssignTask_AsProjectMemberOrViewer_Returns403()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("assign-roles");
            var (_, memberToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Assign Member", ownerToken);
            var (targetId, _) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Assign Target", ownerToken);
            var (_, viewerToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Viewer, "Assign Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Protected assignable task", ownerToken);

            var asMember = await _helpers.AssignTaskRequestAsync(taskId, new { userId = targetId }, memberToken);
            var asViewer = await _helpers.AssignTaskRequestAsync(taskId, new { userId = targetId }, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asViewer.StatusCode);
        }

        [Fact]
        public async Task AssignTask_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("assign-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("assign-outsider-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Private assignable task", ownerToken);

            var response = await _helpers.AssignTaskRequestAsync(taskId, new { userId = Guid.NewGuid() }, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AssignTask_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("assign-notoken");

            var taskId = await _helpers.CreateTaskAsync(projectId, "No token assignable task", ownerToken);

            var response = await _helpers.AssignTaskRequestAsync(taskId, new { userId = Guid.NewGuid() });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AssignTask_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("assign-unknown");

            var response = await _helpers.AssignTaskRequestAsync(long.MaxValue, new { userId = Guid.NewGuid() }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AssignTask_AfterTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("assign-deletedtask");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed assignable task", ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.AssignTaskRequestAsync(taskId, new { userId = Guid.NewGuid() }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AssignTask_WithZeroTaskId_Returns400WithFieldError()
        {
            var (_, _, ownerToken) = await CreateOwnedProjectAsync("assign-zeroid");

            var response = await _helpers.AssignTaskRequestAsync(0, new { userId = Guid.NewGuid() }, ownerToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "TaskId");
        }

        [Fact]
        public async Task AssignTask_WithEmptyUserId_Returns400WithFieldError()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("assign-emptyuser");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Existing task", ownerToken);

            var response = await _helpers.AssignTaskRequestAsync(taskId, new { userId = Guid.Empty }, ownerToken);

            await ValidationAssertions.AssertFieldErrorAsync(response, "UserId");
        }

        [Fact]
        public async Task AssignTask_ForAUserWhoIsNotAProjectMember_Returns404()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("assign-foreignuser");
            var (strangerId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("assign-foreignuser-user");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Foreign assignable task", ownerToken);

            var response = await _helpers.AssignTaskRequestAsync(taskId, new { userId = strangerId }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
