using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskDependecy
{
    [Collection("Projects")]
    public sealed class AddTaskDependencyIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<(long DependentId, long PredecessorId)> CreateDependencyPairAsync(Guid projectId, string ownerToken)
        {
            var predecessorId = await _helpers.CreateTaskAsync(projectId, "Predecessor", ownerToken);
            var dependentId = await _helpers.CreateTaskAsync(projectId, "Dependent", ownerToken);

            return (dependentId, predecessorId);
        }

        [Fact]
        public async Task AddTaskDependency_AsProjectOwner_ReturnsTheDependencyIdAndStoresIt()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add");
            var (dependentId, predecessorId) = await CreateDependencyPairAsync(projectId, ownerToken);

            var dependencyId = await _helpers.AddTaskDependencyAsync(dependentId, predecessorId, ownerToken);

            Assert.True(dependencyId > 0);

            var listed = await _helpers.GetTaskDependenciesResultAsync(dependentId, ownerToken);

            Assert.Equal(1, listed.TotalCount);

            var item = Assert.Single(listed.Items);

            Assert.Equal(dependencyId, item.DependencyId);
            Assert.Equal(dependentId, item.TaskId);
            Assert.Equal(predecessorId, item.DependsOnTaskId);
            Assert.Equal("Predecessor", item.DependsOnTaskTitle);
            Assert.Equal(TaskItemStatus.Todo, item.DependsOnTaskStatus);
            Assert.Equal(TaskPriority.Medium, item.DependsOnTaskPriority);
            Assert.Null(item.DependsOnTaskDueDate);
            Assert.Equal(AuthenticatedUser.DefaultDisplayName, item.CreatedByName);

            var task = await _helpers.GetTaskResultAsync(dependentId, ownerToken);

            Assert.Equal(1, task.DependenciesCount);
        }

        [Fact]
        public async Task AddTaskDependency_AsProjectAdmin_Returns200()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-admin");
            var (_, adminToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Dependency Admin", ownerToken);
            var (dependentId, predecessorId) = await CreateDependencyPairAsync(projectId, ownerToken);

            var response = await _helpers.AddTaskDependencyRequestAsync(dependentId,
                new { dependsOnTaskId = predecessorId }, adminToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddTaskDependency_AsProjectMemberOrViewer_Returns403()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-roles");
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Dependency Member", ownerToken);
            var (_, viewerToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Dependency Viewer", ownerToken);
            var (dependentId, predecessorId) = await CreateDependencyPairAsync(projectId, ownerToken);

            var asMember = await _helpers.AddTaskDependencyRequestAsync(dependentId,
                new { dependsOnTaskId = predecessorId }, memberToken);
            var asViewer = await _helpers.AddTaskDependencyRequestAsync(dependentId,
                new { dependsOnTaskId = predecessorId }, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asViewer.StatusCode);
        }

        [Fact]
        public async Task AddTaskDependency_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("dep-add-outsider-stranger");
            var (dependentId, predecessorId) = await CreateDependencyPairAsync(projectId, ownerToken);

            var response = await _helpers.AddTaskDependencyRequestAsync(dependentId,
                new { dependsOnTaskId = predecessorId }, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddTaskDependency_AsTeamOwnerWithoutProjectMembership_Returns200()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("dep-add-teamowner-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var (dependentId, predecessorId) = await CreateDependencyPairAsync(projectId, ownerToken);

            var response = await _helpers.AddTaskDependencyRequestAsync(dependentId,
                new { dependsOnTaskId = predecessorId }, teamOwnerToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddTaskDependency_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-notoken");
            var (dependentId, predecessorId) = await CreateDependencyPairAsync(projectId, ownerToken);

            var response = await _helpers.AddTaskDependencyRequestAsync(dependentId, new { dependsOnTaskId = predecessorId });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddTaskDependency_ForUnknownTask_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-unknowntask");
            var (_, predecessorId) = await CreateDependencyPairAsync(projectId, ownerToken);

            var response = await _helpers.AddTaskDependencyRequestAsync(long.MaxValue,
                new { dependsOnTaskId = predecessorId }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddTaskDependency_AfterDependentTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-deleteddependent");
            var (dependentId, predecessorId) = await CreateDependencyPairAsync(projectId, ownerToken);

            await _helpers.DeleteTaskAsync(dependentId, ownerToken);

            var response = await _helpers.AddTaskDependencyRequestAsync(dependentId,
                new { dependsOnTaskId = predecessorId }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddTaskDependency_OnUnknownOrDeletedPredecessor_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-badpredecessor");
            var (dependentId, predecessorId) = await CreateDependencyPairAsync(projectId, ownerToken);

            var unknown = await _helpers.AddTaskDependencyRequestAsync(dependentId,
                new { dependsOnTaskId = long.MaxValue }, ownerToken);

            await _helpers.DeleteTaskAsync(predecessorId, ownerToken);

            var deleted = await _helpers.AddTaskDependencyRequestAsync(dependentId,
                new { dependsOnTaskId = predecessorId }, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        }

        [Fact]
        public async Task AddTaskDependency_OnPredecessorFromAnotherProject_Returns400()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-otherproject");
            var (_, otherProjectId) = await _helpers.CreateProjectAsync($"Other {Guid.NewGuid():N}", ownerToken, teamId: teamId);

            var dependentId = await _helpers.CreateTaskAsync(projectId, "Dependent", ownerToken);
            var foreignPredecessorId = await _helpers.CreateTaskAsync(otherProjectId, "Foreign predecessor", ownerToken);

            var response = await _helpers.AddTaskDependencyRequestAsync(dependentId,
                new { dependsOnTaskId = foreignPredecessorId }, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.BadRequest, "same project");
        }

        [Fact]
        public async Task AddTaskDependency_ThatWouldCloseADirectCycle_Returns400()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-cycle2");
            var (dependentId, predecessorId) = await CreateDependencyPairAsync(projectId, ownerToken);

            await _helpers.AddTaskDependencyAsync(dependentId, predecessorId, ownerToken);

            var response = await _helpers.AddTaskDependencyRequestAsync(predecessorId,
                new { dependsOnTaskId = dependentId }, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.BadRequest, "circular");
        }

        [Fact]
        public async Task AddTaskDependency_ThatWouldCloseATransitiveCycle_Returns400()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-cycle3");

            var topTaskId = await _helpers.CreateTaskAsync(projectId, "Top", ownerToken);
            var middleTaskId = await _helpers.CreateTaskAsync(projectId, "Middle", ownerToken);
            var bottomTaskId = await _helpers.CreateTaskAsync(projectId, "Bottom", ownerToken);

            await _helpers.AddTaskDependencyAsync(middleTaskId, bottomTaskId, ownerToken);
            await _helpers.AddTaskDependencyAsync(topTaskId, middleTaskId, ownerToken);

            var response = await _helpers.AddTaskDependencyRequestAsync(bottomTaskId,
                new { dependsOnTaskId = topTaskId }, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.BadRequest, "circular");

            var unchanged = await _helpers.GetTaskDependenciesResultAsync(bottomTaskId, ownerToken);

            Assert.Empty(unchanged.Items);
        }

        [Fact]
        public async Task AddTaskDependency_ForAPredecessorAlreadyReachedTransitively_Returns200()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-add-diamond");

            var topTaskId = await _helpers.CreateTaskAsync(projectId, "Top", ownerToken);
            var middleTaskId = await _helpers.CreateTaskAsync(projectId, "Middle", ownerToken);
            var bottomTaskId = await _helpers.CreateTaskAsync(projectId, "Bottom", ownerToken);

            await _helpers.AddTaskDependencyAsync(middleTaskId, bottomTaskId, ownerToken);
            await _helpers.AddTaskDependencyAsync(topTaskId, middleTaskId, ownerToken);

            var response = await _helpers.AddTaskDependencyRequestAsync(topTaskId,
                new { dependsOnTaskId = bottomTaskId }, ownerToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var listed = await _helpers.GetTaskDependenciesResultAsync(topTaskId, ownerToken);

            Assert.Equal(2, listed.TotalCount);
        }


    }
}
