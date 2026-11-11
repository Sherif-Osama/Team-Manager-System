using System.Net;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Project;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.ProjectsController
{
    [Collection("Projects")]
    public sealed class GetProjectByIdIntegrationTests(ProjectFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        [Fact]
        public async Task GetProjectById_AsOwner_ReturnsProjectWithJoinedNames()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-owner");

            var marker = $"p{Guid.NewGuid():N}";
            var teamName = $"Detail Team {marker}";
            var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);
            var dueDate = startDate.AddDays(15);

            var teamId = await _teamHelpers.CreateTeamAsync(teamName, null, ownerToken);

            var createdBefore = DateTime.UtcNow;
            var (_, projectId) = await _helpers.CreateProjectAsync($"{marker} Detail", ownerToken, startDate, dueDate,
                "Project detail", teamId);

            var project = await _helpers.GetProjectResultAsync(projectId, ownerToken);

            Assert.Equal(projectId, project.Id);
            Assert.Equal(teamId, project.TeamId);
            Assert.Equal(teamName, project.TeamName);
            Assert.Equal($"{marker} Detail", project.ProjectName);
            Assert.Equal("Project detail", project.Description);
            Assert.Equal(ProjectStatus.Active, project.Status);
            Assert.Equal(startDate, project.StartDate);
            Assert.Equal(dueDate, project.DueDate);
            Assert.Equal(ownerId, project.OwnerUserId);
            Assert.Equal(ownerId, project.CreatedBy);
            Assert.Equal(AuthenticatedUser.DefaultDisplayName, project.OwnerName);
            Assert.Equal(AuthenticatedUser.DefaultDisplayName, project.CreatorName);
            Assert.Equal(1, project.MembersCount);
            Assert.Equal(0, project.TasksCount);
            Assert.InRange(project.CreatedAtUtc, createdBefore, DateTime.UtcNow);
        }

        [Fact]
        public async Task GetProjectById_ExcludesRemovedMembersAndCancelledOrDeletedTasksFromCounts()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-counts-owner");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-counts-member");

            var marker = $"p{Guid.NewGuid():N}";
            var (teamId, projectId) = await _helpers.CreateProjectAsync($"{marker} Counts", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            var membershipId = await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Member, ownerToken);

            await _helpers.CreateTaskAsync(projectId, "Open Task", ownerToken);
            var cancelledTaskId = await _helpers.CreateTaskAsync(projectId, "Cancelled Task", ownerToken);
            var deletedTaskId = await _helpers.CreateTaskAsync(projectId, "Deleted Task", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent,
                (await _helpers.ChangeTaskStatusAsync(cancelledTaskId, TaskItemStatus.Cancelled, ownerToken)).StatusCode);
            await _helpers.DeleteTaskAsync(deletedTaskId, ownerToken);

            await _helpers.RemoveProjectMemberAsync(projectId, membershipId, ownerToken);

            var project = await _helpers.GetProjectResultAsync(projectId, ownerToken);

            Assert.Equal(1, project.MembersCount);
            Assert.Equal(1, project.TasksCount);
        }

        [Fact]
        public async Task GetProjectById_AsProjectMember_ReturnsProject()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-member-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-member");

            var marker = $"p{Guid.NewGuid():N}";
            var (teamId, projectId) = await _helpers.CreateProjectAsync($"{marker} Shared", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Member, ownerToken);

            var project = await _helpers.GetProjectResultAsync(projectId, memberToken);

            Assert.Equal(projectId, project.Id);
            Assert.Equal(2, project.MembersCount);
        }

        [Fact]
        public async Task GetProjectById_AsTeamOwnerWithoutProjectMembership_ReturnsProject()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-teamowner-owner");
            var (teamOwnerId, _, teamOwnerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-teamowner");

            var marker = $"p{Guid.NewGuid():N}";
            var teamId = await _teamHelpers.CreateTeamAsync($"Team {marker}", null, ownerToken);
            var (_, projectId) = await _helpers.CreateProjectAsync($"{marker} Owned Project", ownerToken, teamId: teamId);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, teamOwnerId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var transfer = await _helpers.RequestAsync(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId), ownerToken,
                new { newOwnerUserId = teamOwnerId });

            Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

            var project = await _helpers.GetProjectResultAsync(projectId, teamOwnerToken);

            Assert.Equal(projectId, project.Id);
        }

        [Fact]
        public async Task GetProjectById_AsTeamMemberWithoutProjectMembership_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-teammember-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-teammember");

            var marker = $"p{Guid.NewGuid():N}";
            var teamId = await _teamHelpers.CreateTeamAsync($"Team {marker}", null, ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);

            var (_, projectId) = await _helpers.CreateProjectAsync($"{marker} Private", ownerToken, teamId: teamId);

            var response = await _helpers.GetProjectAsync(projectId, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetProjectById_AsOutsider_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-outsider-owner");
            var (_, _, outsiderToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-outsider");

            var (_, projectId) = await _helpers.CreateProjectAsync($"Outsider {Guid.NewGuid():N}", ownerToken);

            var response = await _helpers.GetProjectAsync(projectId, outsiderToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetProjectById_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-notoken");
            var (_, projectId) = await _helpers.CreateProjectAsync($"No Token {Guid.NewGuid():N}", ownerToken);

            var response = await _helpers.GetProjectAsync(projectId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProjectById_WithUnknownProjectId_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-unknown");

            var response = await _helpers.GetProjectAsync(Guid.NewGuid(), ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


        [Fact]
        public async Task GetProjectById_AfterMembershipIsRevoked_DropsCachedAccess()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-revoke-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyid-revoke-member");

            var marker = $"p{Guid.NewGuid():N}";
            var (teamId, projectId) = await _helpers.CreateProjectAsync($"{marker} Revoked", ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            var membershipId = await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Member, ownerToken);

            // The first read caches the membership, so the 403 below proves the removal evicted it.
            Assert.Equal(HttpStatusCode.OK, (await _helpers.GetProjectAsync(projectId, memberToken)).StatusCode);

            await _helpers.RemoveProjectMemberAsync(projectId, membershipId, ownerToken);

            var response = await _helpers.GetProjectAsync(projectId, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
