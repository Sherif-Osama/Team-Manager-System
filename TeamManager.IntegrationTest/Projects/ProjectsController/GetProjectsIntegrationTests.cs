using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.ProjectsController
{
    [Collection("Projects")]
    public sealed class GetProjectsIntegrationTests(ProjectsFixture factory)
    {
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<HttpResponseMessage> GetProjectsAsync(string? accessToken, string query = "")
            => await _helpers.RequestAsync(HttpMethod.Get, $"{ProjectEndpoints.Projects}{query}", accessToken);

        private async Task<GetProjectsResponseDto> GetProjectsResultAsync(string accessToken, string query = "")
        {
            var response = await GetProjectsAsync(accessToken, query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetProjectsResponseDto>())!;
        }

        [Fact]
        public async Task GetProjects_AsAdmin_ReturnsProjectsWithDefaultPaging()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getprojects-admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getprojects-owner");

            var marker = $"p{Guid.NewGuid():N}";
            var (_, firstId) = await _helpers.CreateProjectAsync($"{marker} First", ownerToken);
            var (_, secondId) = await _helpers.CreateProjectAsync($"{marker} Second", ownerToken);

            var result = await GetProjectsResultAsync(adminToken);

            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);
            Assert.True(result.TotalCount >= 2);
            Assert.Contains(result.Projects, p => p.Id == firstId);
            Assert.Contains(result.Projects, p => p.Id == secondId);
        }

        [Fact]
        public async Task GetProjects_AsProjectOwnerWithoutSystemPermission_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getprojects-nopermission");

            await _helpers.CreateProjectAsync($"Owned {Guid.NewGuid():N}", ownerToken);

            var response = await GetProjectsAsync(ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetProjects_AsPlainUser_Returns403()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync("getprojects-plain");

            var response = await GetProjectsAsync(accessToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetProjects_WithoutAccessToken_Returns401()
        {
            var response = await GetProjectsAsync(null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProjects_WithSearchMatchingName_ReturnsOnlyMatchingProjects()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getprojects-name-admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getprojects-name-owner");

            var marker = $"p{Guid.NewGuid():N}";
            var (_, eastId) = await _helpers.CreateProjectAsync($"{marker} East", ownerToken);
            var (_, westId) = await _helpers.CreateProjectAsync($"{marker} West", ownerToken);
            var (_, otherId) = await _helpers.CreateProjectAsync("Unrelated Project", ownerToken);

            var result = await GetProjectsResultAsync(adminToken, $"?search={marker}");

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Projects.Count);
            Assert.Contains(result.Projects, p => p.Id == eastId);
            Assert.Contains(result.Projects, p => p.Id == westId);
            Assert.DoesNotContain(result.Projects, p => p.Id == otherId);
        }

        [Fact]
        public async Task GetProjects_WithSearchMatchingDescription_ReturnsOnlyMatchingProjects()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getprojects-desc-admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getprojects-desc-owner");

            var marker = $"p{Guid.NewGuid():N}";
            var (_, describedId) = await _helpers.CreateProjectAsync("Name Without Marker", ownerToken, description: $"shipped {marker} scope");
            await _helpers.CreateProjectAsync("Another Name Without Marker", ownerToken);

            var result = await GetProjectsResultAsync(adminToken, $"?search={marker}");

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(describedId, result.Projects.Single().Id);
        }

        [Fact]
        public async Task GetProjects_WithStatusFilter_ReturnsOnlyProjectsInThatStatus()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getprojects-status-admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getprojects-status-owner");

            var marker = $"p{Guid.NewGuid():N}";
            var (_, activeId) = await _helpers.CreateProjectAsync($"{marker} Ongoing", ownerToken);
            var (_, completedId) = await _helpers.CreateProjectAsync($"{marker} Finished", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent,
                (await _helpers.ChangeProjectStatusAsync(completedId, ProjectStatus.Completed, ownerToken)).StatusCode);

            var completed = await GetProjectsResultAsync(adminToken, $"?search={marker}&status={(int)ProjectStatus.Completed}");

            Assert.Equal(1, completed.TotalCount);
            Assert.Equal(completedId, completed.Projects.Single().Id);
            Assert.Equal(ProjectStatus.Completed, completed.Projects.Single().Status);

            var active = await GetProjectsResultAsync(adminToken, $"?search={marker}&status={(int)ProjectStatus.Active}");

            Assert.Equal(1, active.TotalCount);
            Assert.Equal(activeId, active.Projects.Single().Id);
        }

        [Fact]
        public async Task GetProjects_WithPagination_ReturnsNewestProjectsPerPage()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getprojects-page-admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getprojects-page-owner");

            var marker = $"p{Guid.NewGuid():N}";
            var (_, oldestId) = await _helpers.CreateProjectAsync($"{marker} One", ownerToken);
            var (_, middleId) = await _helpers.CreateProjectAsync($"{marker} Two", ownerToken);
            var (_, newestId) = await _helpers.CreateProjectAsync($"{marker} Three", ownerToken);

            var firstPage = await GetProjectsResultAsync(adminToken, $"?search={marker}&page=1&pageSize=2");

            Assert.Equal(3, firstPage.TotalCount);
            Assert.Equal(1, firstPage.Page);
            Assert.Equal(2, firstPage.PageSize);
            Assert.Equal(newestId, firstPage.Projects[0].Id);
            Assert.Equal(middleId, firstPage.Projects[1].Id);

            var secondPage = await GetProjectsResultAsync(adminToken, $"?search={marker}&page=2&pageSize=2");

            Assert.Equal(2, secondPage.Page);
            Assert.Single(secondPage.Projects);
            Assert.Equal(oldestId, secondPage.Projects[0].Id);
        }

        [Fact]
        public async Task GetProjects_ExcludesSoftDeletedProjects()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getprojects-deleted-admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getprojects-deleted-owner");

            var marker = $"p{Guid.NewGuid():N}";
            var (_, aliveId) = await _helpers.CreateProjectAsync($"{marker} Alive", ownerToken);
            var (_, deletedId) = await _helpers.CreateProjectAsync($"{marker} Removed", ownerToken);

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, ProjectEndpoints.Project(deletedId), ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var result = await GetProjectsResultAsync(adminToken, $"?search={marker}");

            Assert.Equal(1, result.TotalCount);
            Assert.Equal(aliveId, result.Projects.Single().Id);
        }

        [Fact]
        public async Task GetProjects_ReturnsProjectedFieldsAndActiveCounts()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getprojects-fields-admin");
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getprojects-fields-owner");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("getprojects-fields-member");

            var marker = $"p{Guid.NewGuid():N}";
            var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
            var dueDate = startDate.AddDays(20);
            var teamName = $"Projection Team {marker}";

            var teamId = await _teamHelpers.CreateTeamAsync(teamName, null, ownerToken);

            var create = await _helpers.CreateProjectRequestAsync(teamId, new
            {
                name = $"{marker} Projection",
                description = "Projected description",
                startDate,
                dueDate
            }, ownerToken);

            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            var projectId = await create.Content.ReadFromJsonAsync<Guid>();

            Assert.True((await _teamHelpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken)).IsSuccessStatusCode);
            var membershipId = await _helpers.AddProjectMemberAsync(projectId, memberId, ProjectRole.Member, ownerToken);

            await _helpers.CreateTaskAsync(projectId, "Kept Task", ownerToken);
            var droppedTaskId = await _helpers.CreateTaskAsync(projectId, "Dropped Task", ownerToken);
            await _helpers.DeleteTaskAsync(droppedTaskId, ownerToken);

            await _helpers.RemoveProjectMemberAsync(projectId, membershipId, ownerToken);

            var result = await GetProjectsResultAsync(adminToken, $"?search={marker}");

            var item = result.Projects.Single(p => p.Id == projectId);

            Assert.Equal(teamId, item.TeamId);
            Assert.Equal(teamName, item.TeamName);
            Assert.Equal($"{marker} Projection", item.ProjectName);
            Assert.Equal("Projected description", item.Description);
            Assert.Equal(ProjectStatus.Active, item.Status);
            Assert.Equal(startDate, item.StartDate);
            Assert.Equal(dueDate, item.DueDate);
            Assert.Equal(ownerId, item.OwnerUserId);
            Assert.Equal(ownerId, item.CreatedBy);
            Assert.Equal(AuthenticatedUser.DefaultDisplayName, item.OwnerName);
            Assert.Equal(AuthenticatedUser.DefaultDisplayName, item.CreatorName);
            Assert.Equal(1, item.MembersCount);
            Assert.Equal(1, item.TasksCount);
        }

        [Fact]
        public async Task GetProjects_WithInvalidPaging_Returns400WithFieldErrors()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getprojects-paging-admin");

            var zeroPage = await GetProjectsAsync(adminToken, "?page=0");
            await ValidationAssertions.AssertFieldErrorAsync(zeroPage, "Page");

            var oversizedPage = await GetProjectsAsync(adminToken, "?pageSize=101");
            await ValidationAssertions.AssertFieldErrorAsync(oversizedPage, "PageSize");
        }

        [Fact]
        public async Task GetProjects_WithUnknownStatus_Returns400WithFieldError()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getprojects-badstatus-admin");

            var response = await GetProjectsAsync(adminToken, $"?status={(int)ProjectStatus.Archived + 1}");

            await ValidationAssertions.AssertFieldErrorAsync(response, "Status");
        }

        [Fact]
        public async Task GetProjects_WithTooLongSearch_Returns400WithFieldError()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("getprojects-longsearch-admin");

            var response = await GetProjectsAsync(adminToken, $"?search={new string('x', 151)}");

            await ValidationAssertions.AssertFieldErrorAsync(response, "Search");
        }
    }
}
