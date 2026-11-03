using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamsController
{
    [Collection("Team")]
    public sealed class TeamStatusIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<HttpResponseMessage> ChangeStatusAsync(Guid teamId, string action, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, action == "deactivate"
                ? TeamEndpoints.TeamDeactivate(teamId) : TeamEndpoints.TeamActivate(teamId));

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task DeactivateTeam_AsOwner_DeactivatesTeam()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-owner");
            var teamId = await _helpers.CreateTeamAsync("Status Team One", null, ownerToken);

            var response = await ChangeStatusAsync(teamId, "deactivate", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var team = await db.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId);

            Assert.False(team.IsActive);
            Assert.NotNull(team.UpdatedAtUtc);
        }

        [Fact]
        public async Task DeactivateTeam_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-member");

            var teamId = await _helpers.CreateTeamAsync("Status Team Two", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await ChangeStatusAsync(teamId, "deactivate", memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeactivateTeam_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-notoken");
            var teamId = await _helpers.CreateTeamAsync("Status Team Three", null, ownerToken);

            var response = await ChangeStatusAsync(teamId, "deactivate");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeactivateTeam_WithUnknownTeamId_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-unknown");

            var response = await ChangeStatusAsync(Guid.NewGuid(), "deactivate", ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ActivateTeam_AsOwner_ActivatesTeam()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-activate");
            var teamId = await _helpers.CreateTeamAsync("Status Team Four", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.Deactivate();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await ChangeStatusAsync(teamId, "activate", ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var team2 = await db2.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId);

            Assert.True(team2.IsActive);
        }

        [Fact]
        public async Task ActivateTeam_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-member");

            var teamId = await _helpers.CreateTeamAsync("Status Team Five", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await ChangeStatusAsync(teamId, "activate", memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeactivateTeam_OnDeletedTeam_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("status-deleted");
            var teamId = await _helpers.CreateTeamAsync("Status Team Six", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await ChangeStatusAsync(teamId, "deactivate", ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
