using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamsController
{
    [Collection("Team")]
    public sealed class SoftDeleteTeamIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<HttpResponseMessage> DeleteTeamAsync(Guid teamId, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, TeamEndpoints.Team(teamId));

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task SoftDeleteTeam_AsOwner_SoftDeletesTeam()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("softdelete-owner");
            var teamId = await _helpers.CreateTeamAsync("SoftDelete Team One", null, ownerToken);

            var response = await DeleteTeamAsync(teamId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var team = await db.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId);

            Assert.NotNull(team.DeletedAtUtc);
            Assert.False(team.IsActive);

            var getAfterDelete = await _helpers.GetTeamAsync(teamId, ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, getAfterDelete.StatusCode);
        }

        [Fact]
        public async Task SoftDeleteTeam_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("softdelete-notoken");
            var teamId = await _helpers.CreateTeamAsync("SoftDelete Team Two", null, ownerToken);

            var response = await DeleteTeamAsync(teamId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SoftDeleteTeam_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("softdelete-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("softdelete-member");

            var teamId = await _helpers.CreateTeamAsync("SoftDelete Team Three", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await DeleteTeamAsync(teamId, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.False(await db2.Teams.AnyAsync(t => t.Id == teamId && t.DeletedAtUtc != null));
        }

        [Fact]
        public async Task SoftDeleteTeam_WithUnknownTeamId_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("softdelete-unknown");

            var response = await DeleteTeamAsync(Guid.NewGuid(), ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SoftDeleteTeam_WithActiveProjects_Returns409AndKeepsTeam()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("softdelete-projects");
            var teamId = await _helpers.CreateTeamAsync("SoftDelete Team Four", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                db.Projects.Add(new Project(Guid.NewGuid(), teamId, "Active Seed Project", ownerId));
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await DeleteTeamAsync(teamId, ownerToken);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var team = await db2.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId);

            Assert.Null(team.DeletedAtUtc);
            Assert.True(team.IsActive);
        }

        [Fact]
        public async Task SoftDeleteTeam_OnDeletedTeam_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("softdelete-deleted");
            var teamId = await _helpers.CreateTeamAsync("SoftDelete Team Five", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await DeleteTeamAsync(teamId, ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
