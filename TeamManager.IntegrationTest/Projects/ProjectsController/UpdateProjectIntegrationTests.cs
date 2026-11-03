using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Projects.Configuration;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.ProjectsController
{
    [Collection("Projects")]
    public sealed class UpdateProjectIntegrationTests(ProjectsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly ProjectTestHelpers _helpers = new(factory);
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<(Guid TeamId, Guid ProjectId)> CreateProjectAsync(string name, string ownerToken)
        {
            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);
            var response = await _helpers.CreateProjectAsync(teamId, new { name, description = (string?)null }, ownerToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (teamId, await response.Content.ReadFromJsonAsync<Guid>());
        }

        private async Task<HttpResponseMessage> UpdateProjectAsync(Guid projectId, string name, string? description,
            string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, ProjectEndpoints.Project(projectId))
            {
                Content = JsonContent.Create(new { name, description })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task UpdateProject_AsOwner_UpdatesNameAndDescription()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("upd-owner");
            var (_, projectId) = await CreateProjectAsync("Original Project", ownerToken);

            var response = await UpdateProjectAsync(projectId, "Updated Project", "Updated Description", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var project = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == projectId);

            Assert.Equal("Updated Project", project.Name);
            Assert.Equal("Updated Description", project.Description);
        }

        [Fact]
        public async Task UpdateProject_AsProjectMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("upd-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("upd-member");

            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);
            var create = await _helpers.CreateProjectAsync(teamId, new { name = "Member Project", description = (string?)null }, ownerToken);
            var projectId = await create.Content.ReadFromJsonAsync<Guid>();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var project = await db.Projects.SingleAsync(p => p.Id == projectId);
                project.AddMember(memberId, ProjectRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await UpdateProjectAsync(projectId, "Hijacked", null, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProject_AsNonMember_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("upd-owner");
            var (_, _, outsiderToken) = await _authenticatedUser.CreateConfirmedUserAsync("upd-outsider");
            var (_, projectId) = await CreateProjectAsync("Hidden Project", ownerToken);

            var response = await UpdateProjectAsync(projectId, "Stolen", null, outsiderToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProject_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("upd-notoken");
            var (_, projectId) = await CreateProjectAsync("No Token Project", ownerToken);

            var response = await UpdateProjectAsync(projectId, "No Token", null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProject_WithUnknownProjectId_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("upd-unknown");

            var response = await UpdateProjectAsync(Guid.NewGuid(), "Ghost", null, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProject_WithDuplicateNameInSameTeam_Returns409()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("upd-dup");

            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);

            var first = await _helpers.CreateProjectAsync(teamId, new { name = "Existing Project", description = (string?)null }, ownerToken);
            var second = await _helpers.CreateProjectAsync(teamId, new { name = "Rename Target", description = (string?)null }, ownerToken);
            var targetId = await second.Content.ReadFromJsonAsync<Guid>();

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var response = await UpdateProjectAsync(targetId, "existing project", null, ownerToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProject_WithSameNameInOtherTeam_IsAllowed()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("upd-multiteam");

            var team1 = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);
            var team2 = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);

            await _helpers.CreateProjectAsync(team1, new { name = "Shared Name", description = (string?)null }, ownerToken);
            var second = await _helpers.CreateProjectAsync(team2, new { name = "Other Name", description = (string?)null }, ownerToken);
            var targetId = await second.Content.ReadFromJsonAsync<Guid>();

            var response = await UpdateProjectAsync(targetId, "Shared Name", null, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProject_WithInvalidPayload_Returns400WithFieldErrors()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("upd-invalid");
            var (_, projectId) = await CreateProjectAsync("Validation Project", ownerToken);

            var response = await UpdateProjectAsync(projectId, "", null, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("Name", out _));
        }
    }
}
