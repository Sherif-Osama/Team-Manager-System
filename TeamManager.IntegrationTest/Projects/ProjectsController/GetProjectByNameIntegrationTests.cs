using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Project;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.ProjectsController
{
    [Collection("Projects")]
    public sealed class GetProjectByNameIntegrationTests(ProjectFactory factory) : IntegrationTestBase(factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<ProjectResponseDto> GetProjectByNameResultAsync(string name, string accessToken)
        {
            var response = await _helpers.GetProjectByNameAsync(name, accessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<ProjectResponseDto>())!;
        }

        [Fact]
        public async Task GetProjectByName_AsAdmin_ReturnsProjectWithJoinedFields()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getbyname-admin");
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyname-owner");

            var marker = $"p{Guid.NewGuid():N}";
            var projectName = $"{marker} Detail";
            var teamName = $"Named Team {marker}";
            var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(4);
            var dueDate = startDate.AddDays(12);

            var teamId = await _teamHelpers.CreateTeamAsync(teamName, null, ownerToken);

            var createdBefore = DateTime.UtcNow;
            await _helpers.CreateProjectAsync(projectName, ownerToken, startDate, dueDate, "ByName projection", teamId);

            var project = await GetProjectByNameResultAsync(projectName, adminToken);

            Assert.Equal(teamId, project.TeamId);
            Assert.Equal(teamName, project.TeamName);
            Assert.Equal(projectName, project.ProjectName);
            Assert.Equal("ByName projection", project.Description);
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
        public async Task GetProjectByName_AsProjectOwnerWithoutSystemPermission_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyname-nopermission");

            var marker = $"p{Guid.NewGuid():N}";
            await _helpers.CreateProjectAsync($"{marker} Owned", ownerToken);

            var response = await _helpers.GetProjectByNameAsync($"{marker} Owned", ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetProjectByName_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyname-notoken");

            var marker = $"p{Guid.NewGuid():N}";
            await _helpers.CreateProjectAsync($"{marker} Public", ownerToken);

            var response = await _helpers.GetProjectByNameAsync($"{marker} Public");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProjectByName_WithUnknownName_Returns404()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getbyname-unknown-admin");

            var response = await _helpers.GetProjectByNameAsync($"missing-{Guid.NewGuid():N}", adminToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetProjectByName_WithPartialName_Returns404()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getbyname-partial-admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyname-partial-owner");

            var marker = $"p{Guid.NewGuid():N}";
            await _helpers.CreateProjectAsync($"{marker} Alpha", ownerToken);

            var prefixOnly = await _helpers.GetProjectByNameAsync(marker, adminToken);

            Assert.Equal(HttpStatusCode.NotFound, prefixOnly.StatusCode);

            var truncatedName = await _helpers.GetProjectByNameAsync($"{marker} Alph", adminToken);

            Assert.Equal(HttpStatusCode.NotFound, truncatedName.StatusCode);
        }



        [Fact]
        public async Task GetProjectByName_WithEncodedSpecialCharacters_ReturnsProject()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getbyname-encoded-admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyname-encoded-owner");

            var projectName = $"p{Guid.NewGuid():N} & Trust/Load 100%";
            await _helpers.CreateProjectAsync(projectName, ownerToken);

            var project = await GetProjectByNameResultAsync(projectName, adminToken);

            Assert.Equal(projectName, project.ProjectName);
        }

        [Fact]
        public async Task GetProjectByName_AfterProjectIsSoftDeleted_Returns404()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getbyname-softdelete-admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyname-softdelete-owner");

            var marker = $"p{Guid.NewGuid():N}";
            var projectName = $"{marker} Archived";
            var (_, projectId) = await _helpers.CreateProjectAsync(projectName, ownerToken);

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(projectId), ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var response = await _helpers.GetProjectByNameAsync(projectName, adminToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetProjectByName_CountsExcludeRemovedMembersAndDeletedTasksOnly()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyname-counts-owner");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("getbyname-counts-member");
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getbyname-counts-admin");

            var marker = $"p{Guid.NewGuid():N}";
            var projectName = $"{marker} Counts";
            var (teamId, projectId) = await _helpers.CreateProjectAsync(projectName, ownerToken);

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            var membershipId = await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Member, ownerToken);

            await _helpers.CreateTaskAsync(projectId, "Open Task", ownerToken);
            var cancelledTaskId = await _helpers.CreateTaskAsync(projectId, "Cancelled Task", ownerToken);
            var deletedTaskId = await _helpers.CreateTaskAsync(projectId, "Deleted Task", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent,
                (await _helpers.ChangeTaskStatusAsync(cancelledTaskId, TaskItemStatus.Cancelled, ownerToken)).StatusCode);
            await _helpers.DeleteTaskAsync(deletedTaskId, ownerToken);

            await _helpers.RemoveProjectMemberAsync(projectId, membershipId, ownerToken);

            var project = await GetProjectByNameResultAsync(projectName, adminToken);

            Assert.Equal(1, project.MembersCount);
            Assert.Equal(2, project.TasksCount);
        }

        [Fact]
        public async Task GetProjectByName_WhenTheSameNameExistsInTwoTeams_ReturnsOneOfThem()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getbyname-duplicate-admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getbyname-duplicate-owner");

            var marker = $"p{Guid.NewGuid():N}";
            var sharedName = $"{marker} Duplicated";

            var (_, firstId) = await _helpers.CreateProjectAsync(sharedName, ownerToken);
            var (_, secondId) = await _helpers.CreateProjectAsync(sharedName, ownerToken);

            var project = await GetProjectByNameResultAsync(sharedName, adminToken);

            Assert.Equal(sharedName, project.ProjectName);
            Assert.Contains(new[] { firstId, secondId }, id => id == project.Id);
        }
    }
}
