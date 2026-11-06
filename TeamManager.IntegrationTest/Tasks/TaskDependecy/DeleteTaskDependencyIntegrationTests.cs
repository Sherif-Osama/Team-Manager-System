using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Tasks.TaskDependecy
{
    [Collection("Projects")]
    public sealed class DeleteTaskDependencyIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<(long DependentId, long PredecessorId, long DependencyId)> CreateLinkedPairAsync(Guid projectId,
            string ownerToken)
        {
            var predecessorId = await _helpers.CreateTaskAsync(projectId, "Predecessor", ownerToken);
            var dependentId = await _helpers.CreateTaskAsync(projectId, "Dependent", ownerToken);
            var dependencyId = await _helpers.AddTaskDependencyAsync(dependentId, predecessorId, ownerToken);

            return (dependentId, predecessorId, dependencyId);
        }

        [Fact]
        public async Task DeleteTaskDependency_AsProjectOwner_RemovesTheDependencyAndClearsTheCount()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-del");
            var (dependentId, _, dependencyId) = await CreateLinkedPairAsync(projectId, ownerToken);

            var response = await _helpers.DeleteTaskDependencyRequestAsync(dependentId, dependencyId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var listed = await _helpers.GetTaskDependenciesResultAsync(dependentId, ownerToken);

            Assert.Empty(listed.Items);
            Assert.Equal(0, listed.TotalCount);

            var task = await _helpers.GetTaskResultAsync(dependentId, ownerToken);

            Assert.Equal(0, task.DependenciesCount);
        }

        [Fact]
        public async Task DeleteTaskDependency_AsProjectAdmin_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-del-admin");
            var (_, adminToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Admin,
                "Dependency Delete Admin", ownerToken);
            var (dependentId, _, dependencyId) = await CreateLinkedPairAsync(projectId, ownerToken);

            var response = await _helpers.DeleteTaskDependencyRequestAsync(dependentId, dependencyId, adminToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTaskDependency_AsProjectMemberOrViewer_Returns403AndKeepsTheDependency()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-del-roles");
            var (_, memberToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Member,
                "Dependency Delete Member", ownerToken);
            var (_, viewerToken) = await _helpers.AddProjectMemberAsNewUserAsync(teamId, projectId, ProjectRole.Viewer,
                "Dependency Delete Viewer", ownerToken);
            var (dependentId, _, dependencyId) = await CreateLinkedPairAsync(projectId, ownerToken);

            var asMember = await _helpers.DeleteTaskDependencyRequestAsync(dependentId, dependencyId, memberToken);
            var asViewer = await _helpers.DeleteTaskDependencyRequestAsync(dependentId, dependencyId, viewerToken);

            Assert.Equal(HttpStatusCode.Forbidden, asMember.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, asViewer.StatusCode);

            var listed = await _helpers.GetTaskDependenciesResultAsync(dependentId, ownerToken);

            Assert.Equal(dependencyId, Assert.Single(listed.Items).DependencyId);
        }

        [Fact]
        public async Task DeleteTaskDependency_AsUserOutsideTheProject_Returns403()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-del-outsider");
            var (_, _, strangerToken) = await _authenticatedUser.CreateConfirmedUserAsync("dep-del-outsider-stranger");
            var (dependentId, _, dependencyId) = await CreateLinkedPairAsync(projectId, ownerToken);

            var response = await _helpers.DeleteTaskDependencyRequestAsync(dependentId, dependencyId, strangerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTaskDependency_AsTeamOwnerWithoutProjectMembership_Returns204()
        {
            var (teamId, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-del-teamowner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("dep-del-teamowner-target");

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var (dependentId, _, dependencyId) = await CreateLinkedPairAsync(projectId, ownerToken);

            var response = await _helpers.DeleteTaskDependencyRequestAsync(dependentId, dependencyId, teamOwnerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTaskDependency_WithoutAccessToken_Returns401()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-del-notoken");
            var (dependentId, _, dependencyId) = await CreateLinkedPairAsync(projectId, ownerToken);

            var response = await _helpers.DeleteTaskDependencyRequestAsync(dependentId, dependencyId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTaskDependency_ForUnknownTask_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-del-unknowntask");
            var (_, _, dependencyId) = await CreateLinkedPairAsync(projectId, ownerToken);

            var response = await _helpers.DeleteTaskDependencyRequestAsync(long.MaxValue, dependencyId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTaskDependency_AfterDependentTaskIsDeleted_Returns404()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-del-deletedtask");
            var (dependentId, _, dependencyId) = await CreateLinkedPairAsync(projectId, ownerToken);

            await _helpers.DeleteTaskAsync(dependentId, ownerToken);

            var response = await _helpers.DeleteTaskDependencyRequestAsync(dependentId, dependencyId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTaskDependency_ForAnotherTasksDependency_Returns400AndKeepsIt()
        {
            var (_, projectId, ownerToken) = await _helpers.CreateOwnedProjectAsync("dep-del-foreign");
            var (firstDependentId, _, firstDependencyId) = await CreateLinkedPairAsync(projectId, ownerToken);
            var (secondDependentId, _, secondDependencyId) = await CreateLinkedPairAsync(projectId, ownerToken);

            var response = await _helpers.DeleteTaskDependencyRequestAsync(secondDependentId, firstDependencyId, ownerToken);

            await ProblemDetailsAssertions.AssertDetailAsync(response, HttpStatusCode.BadRequest, "dependency not found");

            var kept = await _helpers.GetTaskDependenciesResultAsync(firstDependentId, ownerToken);

            Assert.Equal(firstDependencyId, Assert.Single(kept.Items).DependencyId);
        }


    }
}
