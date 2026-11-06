using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TasksController
{
    [Collection("Projects")]
    public sealed class RescheduleTaskIntegrationTests(ProjectsFixture factory)
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

        // The predecessor is dated in three days, the task that depends on it in five.
        private async Task<(long PredecessorId, long DependentId, DateOnly PredecessorDueDate, DateOnly DependentDueDate)>
            CreateDatedDependencyPairAsync(Guid projectId, string ownerToken)
        {
            var predecessorDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
            var predecessorId = await _helpers.CreateTaskAsync(projectId, "Predecessor", ownerToken, dueDate: predecessorDueDate);

            var dependentDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
            var dependentId = await _helpers.CreateTaskAsync(projectId, "Early dependent", ownerToken, dueDate: dependentDueDate);

            await _helpers.AddTaskDependencyAsync(dependentId, predecessorId, ownerToken);

            return (predecessorId, dependentId, predecessorDueDate, dependentDueDate);
        }

        [Fact]
        public async Task RescheduleTask_AsProjectOwner_StoresTheNewDates()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("reschedule");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Schedulable task", ownerToken);

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var dueDate = startDate.AddDays(5);

            var response = await _helpers.RescheduleTaskRequestAsync(taskId, new { startDate, dueDate }, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(startDate, task.StartDate);
            Assert.Equal(dueDate, task.DueDate);
        }

        [Fact]
        public async Task RescheduleTask_AsProjectAdmin_StoresTheNewDates()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("reschedule-admin");
            var (_, adminToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Admin, "Reschedule Admin", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Admin schedulable task", ownerToken);

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

            var response = await _helpers.RescheduleTaskRequestAsync(taskId, new { startDate }, adminToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var task = await _helpers.GetTaskResultAsync(taskId, ownerToken);

            Assert.Equal(startDate, task.StartDate);
        }

        [Fact]
        public async Task RescheduleTask_AsProjectMemberOrViewer_Returns403()
        {
            var (teamId, projectId, ownerToken) = await CreateOwnedProjectAsync("reschedule-roles");
            var (_, memberToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Member, "Reschedule Member", ownerToken);
            var (_, viewerToken) = await AddMemberAsync(teamId, projectId, ProjectRole.Viewer, "Reschedule Viewer", ownerToken);

            var taskId = await _helpers.CreateTaskAsync(projectId, "Protected schedule task", ownerToken);

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

            var asMember = await _helpers.RescheduleTaskRequestAsync(taskId, new { startDate }, memberToken);
            var asViewer = await _helpers.RescheduleTaskRequestAsync(taskId, new { startDate }, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asViewer.StatusCode);
        }

        [Fact]
        public async Task RescheduleTask_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("reschedule-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("reschedule-outsider-stranger");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Private schedule task", ownerToken);

            var response = await _helpers.RescheduleTaskRequestAsync(taskId,
                new { startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) }, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RescheduleTask_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("reschedule-notoken");

            var taskId = await _helpers.CreateTaskAsync(projectId, "No token schedule task", ownerToken);

            var response = await _helpers.RescheduleTaskRequestAsync(taskId,
                new { startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RescheduleTask_ForUnknownTask_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("reschedule-unknown");

            var response = await _helpers.RescheduleTaskRequestAsync(long.MaxValue,
                new { startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RescheduleTask_AfterProjectIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("reschedule-deletedproject");

            var taskId = await _helpers.CreateTaskAsync(projectId, "Orphan schedule task", ownerToken);

            var cancel = await _helpers.ChangeTaskStatusAsync(taskId, TaskItemStatus.Cancelled, ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var response = await _helpers.RescheduleTaskRequestAsync(taskId,
                new { startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }




        [Fact]
        public async Task RescheduleTask_WithDueDateAfterADependentsDueDate_Returns409AndKeepsTheDates()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("reschedule-depblocked");
            var (predecessorId, _, predecessorDueDate, dependentDueDate) =
                await CreateDatedDependencyPairAsync(projectId, ownerToken);

            var response = await _helpers.RescheduleTaskRequestAsync(predecessorId,
                new { dueDate = dependentDueDate.AddDays(5) }, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Conflict, "Early dependent");

            var task = await _helpers.GetTaskResultAsync(predecessorId, ownerToken);

            Assert.Equal(predecessorDueDate, task.DueDate);
        }

        [Fact]
        public async Task RescheduleTask_WhenTheDependentIsCancelled_AllowsTheLaterDueDate()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("reschedule-depcancelled");
            var (predecessorId, dependentId, _, dependentDueDate) = await CreateDatedDependencyPairAsync(projectId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent,
                (await _helpers.ChangeTaskStatusAsync(dependentId, TaskItemStatus.Cancelled, ownerToken)).StatusCode);

            var newDueDate = dependentDueDate.AddDays(5);

            var response = await _helpers.RescheduleTaskRequestAsync(predecessorId, new { dueDate = newDueDate }, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var task = await _helpers.GetTaskResultAsync(predecessorId, ownerToken);

            Assert.Equal(newDueDate, task.DueDate);
        }

        [Fact]
        public async Task RescheduleTask_WhenTheDependentIsDone_StillReturns409()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("reschedule-depdone");
            var (predecessorId, dependentId, _, dependentDueDate) = await CreateDatedDependencyPairAsync(projectId, ownerToken);

            await _helpers.CompleteTaskAsync(predecessorId, ownerToken);
            await _helpers.CompleteTaskAsync(dependentId, ownerToken);

            var response = await _helpers.RescheduleTaskRequestAsync(predecessorId,
                new { dueDate = dependentDueDate.AddDays(5) }, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.Conflict, "Early dependent");
        }

        [Fact]
        public async Task RescheduleTask_WithoutDueDate_DoesNotCheckDependents()
        {
            var (_, projectId, ownerToken) = await CreateOwnedProjectAsync("reschedule-dependentsonlystart");
            var (predecessorId, _, _, _) = await CreateDatedDependencyPairAsync(projectId, ownerToken);

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

            var response = await _helpers.RescheduleTaskRequestAsync(predecessorId, new { startDate }, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var task = await _helpers.GetTaskResultAsync(predecessorId, ownerToken);

            Assert.Equal(startDate, task.StartDate);
        }
    }
}
