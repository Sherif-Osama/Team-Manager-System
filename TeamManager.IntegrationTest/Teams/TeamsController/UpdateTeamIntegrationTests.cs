using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamsController
{
    [Collection("Team")]
    public sealed class UpdateTeamIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new AuthenticatedUser(factory);

        private static HttpRequestMessage UpdateTeamRequest(Guid teamId, string name, string? description, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, TeamEndpoints.Team(teamId))
            {
                Content = JsonContent.Create(new { name, description })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return request;
        }

        [Fact]
        public async Task UpdateTeam_ValidRequest_UpdatesNameAndDescription()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync();
            var teamId = await _helpers.CreateTeamAsync("Original Name", "Original Description", accessToken);

            var response = await _client.SendAsync(
                UpdateTeamRequest(teamId, "  Updated Name  ", "Updated Description", accessToken));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var team = await db.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId);

            Assert.Equal("Updated Name", team.Name);
            Assert.Equal("Updated Description", team.Description);
        }

        [Fact]
        public async Task UpdateTeam_WithoutAccessToken_Returns401()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync();
            var teamId = await _helpers.CreateTeamAsync("No Token Team Update", null, accessToken);

            var response = await _client.SendAsync(UpdateTeamRequest(teamId, "Hijacked", null));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTeam_AsNonMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();
            var (_, _, outsiderToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var teamId = await _helpers.CreateTeamAsync("Owned Team", null, ownerToken);

            var response = await _client.SendAsync(UpdateTeamRequest(teamId, "Stolen", null, outsiderToken));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var team = await db.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId);

            Assert.Equal("Owned Team", team.Name);
            Assert.Equal(ownerId, team.OwnerUserId);
        }

        [Fact]
        public async Task UpdateTeam_AsMember_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var teamId = await _helpers.CreateTeamAsync("Member Team", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _client.SendAsync(UpdateTeamRequest(teamId, "Changed By Member", null, memberToken));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var team2 = await db2.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId);

            Assert.Equal("Member Team", team2.Name);
        }

        [Fact]
        public async Task UpdateTeam_WithUnknownTeamId_Returns403()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var response = await _client.SendAsync(
                UpdateTeamRequest(Guid.NewGuid(), "Some Name", null, accessToken));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTeam_WithDuplicateName_Returns409AndKeepsOriginal()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var existingId = await _helpers.CreateTeamAsync("Existing Team", null, accessToken);
            var targetId = await _helpers.CreateTeamAsync("Another Team", null, accessToken);

            var response = await _client.SendAsync(UpdateTeamRequest(targetId, "existing team", null, accessToken));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var target = await db.Teams.AsNoTracking().SingleAsync(t => t.Id == targetId);
            var existing = await db.Teams.AsNoTracking().SingleAsync(t => t.Id == existingId);

            Assert.Equal("Another Team", target.Name);
            Assert.Equal("Existing Team", existing.Name);
        }

        [Fact]
        public async Task UpdateTeam_WithSameName_Returns204()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync();
            var teamId = await _helpers.CreateTeamAsync("Unchanged Team", null, accessToken);

            var response = await _client.SendAsync(UpdateTeamRequest(teamId, "Unchanged Team", null, accessToken));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTeam_WithInvalidPayload_Returns400WithFieldErrors()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync();
            var teamId = await _helpers.CreateTeamAsync("Validation Team", null, accessToken);

            var response = await _client.SendAsync(UpdateTeamRequest(teamId, "", null, accessToken));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("Name", out _));
        }

        [Fact]
        public async Task UpdateTeam_OnDeletedTeam_Returns403()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync();
            var teamId = await _helpers.CreateTeamAsync("Deleted Team", null, accessToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _client.SendAsync(UpdateTeamRequest(teamId, "Resurrected", null, accessToken));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
