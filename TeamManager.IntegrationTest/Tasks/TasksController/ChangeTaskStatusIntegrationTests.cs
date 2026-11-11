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
    public sealed class ChangeTaskStatusIntegrationTests(TaskItemFactory factory) : IntegrationTestBase(factory)
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
        public async Task ChangeTaskStatus_AsProjectOwner_ChangesStatusAndStoresIt()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Owner status task", ownerToken);

            var response = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.InProgress, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(TaskItemStatus.InProgress, task.Status);
        }

        [Fact]
        public async Task ChangeTaskStatus_AsProjectAdmin_ChangesStatus()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-admin");
            var (_, adminToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Admin, "Status Admin", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Admin status task", ownerToken);

            var response = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.InProgress, adminToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskStatus_AsProjectMember_ForTasksTheyAreNotAssignedTo_Returns403()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-member");
            var (_, memberToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Status Member", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Unassigned task", ownerToken);

            var response = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.InProgress, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskStatus_AsAssigneeMember_CanMoveThroughWorkflowStates()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-assignee");
            var (memberId, memberToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Status Assignee", ownerToken);

            var created = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Assigned task", assigneeUserId = memberId, priority = TaskPriority.Medium }, ownerToken);

            var taskId = await created.Content.ReadFromJsonAsync<long>();

            var toInProgress = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.InProgress, memberToken);
            var toInReview = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.InReview, memberToken);
            var backToInProgress = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.InProgress, memberToken);

            Assert.Equal(HttpStatusCode.NoContent, toInProgress.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, toInReview.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, backToInProgress.StatusCode);

            var task = await _helpers.GetTaskResultAsync(taskId, memberToken);

            Assert.Equal(TaskItemStatus.InProgress, task.Status);
        }

        [Fact]
        public async Task ChangeTaskStatus_AsAssigneeMember_CannotPerformNonWorkflowTransitions_Returns403()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-assignee-blocked");
            var (memberId, memberToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Blocked Assignee", ownerToken);

            var created = await _helpers.CreateTaskRequestAsync(projectId,
                new { title = "Assigned task", assigneeUserId = memberId, priority = TaskPriority.Medium }, ownerToken);

            var taskId = await created.Content.ReadFromJsonAsync<long>();

            var response = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.Done, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskStatus_AsTeamOwnerWithoutProjectMembership_Returns403()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("taskstatus-teamowner");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });
            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Team owner status task", ownerToken);

            var response = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.InProgress, teamOwnerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskStatus_AsProjectViewer_Returns403()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-viewer");
            var (_, viewerToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Viewer, "Status Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Viewer status task", ownerToken);

            var response = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.InProgress, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskStatus_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("taskstatus-outsider-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Private status task", ownerToken);

            var response = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.InProgress, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskStatus_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-notoken");

            var taskId = await _helpers.CreateTaskAsync(projectId, "No token status task", ownerToken);

            var response = await _helpers.ChangeTaskStatusRequestAsync(taskId, new { status = TaskItemStatus.InProgress });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskStatus_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("taskstatus-unknown");

            var response = await _helpers.ChangeTaskStatusAsync(long.MaxValue, TaskItemStatus.InProgress, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskStatus_AfterTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-deletedtask");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Doomed status task", ownerToken);
            await _helpers.DeleteTaskAsync(taskId, ownerToken);

            var response = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.InProgress, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }



        [Fact]
        public async Task ChangeTaskStatus_ToDone_WithAnOpenDependency_Returns409AndKeepsTheStatus()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-depopen");

            var blockerId = await _helpers.CreateTaskAsync(projectId, "Blocking predecessor", ownerToken);
            var taskId = await _helpers.CreateTaskAsync(projectId, "Dependent task", ownerToken);

            await _helpers.AddTaskDependencyAsync(taskId, blockerId, ownerToken);
            await _helpers.MoveTaskThroughWorkflowAsync(taskId, ownerToken);

            var response = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.Done, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Conflict, "Blocking predecessor");

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(TaskItemStatus.InReview, task.Status);
            Assert.Null(task.CompletedAtUtc);
        }

        [Fact]
        public async Task ChangeTaskStatus_ToDone_WhenTheDependencyIsDone_Returns204()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-depdone");

            var blockerId = await _helpers.CreateTaskAsync(projectId, "Finished predecessor", ownerToken);
            var taskId = await _helpers.CreateTaskAsync(projectId, "Dependent task", ownerToken);

            await _helpers.AddTaskDependencyAsync(taskId, blockerId, ownerToken);

            await _helpers.CompleteTaskAsync(blockerId, ownerToken);
            await _helpers.MoveTaskThroughWorkflowAsync(taskId, ownerToken);

            var response = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.Done, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ChangeTaskStatus_ToDone_WhenTheDependencyIsCancelled_Returns204()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("taskstatus-depcancelled");

            var blockerId = await _helpers.CreateTaskAsync(projectId, "Cancelled predecessor", ownerToken);
            var taskId = await _helpers.CreateTaskAsync(projectId, "Dependent task", ownerToken);

            await _helpers.AddTaskDependencyAsync(taskId, blockerId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent,
                (await _helpers.ChangeTaskStatusAsync(blockerId, TaskItemStatus.Cancelled, ownerToken)).StatusCode);

            await _helpers.MoveTaskThroughWorkflowAsync(taskId, ownerToken);

            var response = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.Done, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
