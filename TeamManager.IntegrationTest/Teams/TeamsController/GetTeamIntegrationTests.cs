using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Team;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamsController
{
    [Collection("Teams")]
    public sealed class GetTeamIntegrationTests(TeamFactory factory) : IntegrationTestBase(factory)
    {
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new AuthenticatedUser(factory);

        private sealed record GetTeamResponseDto(Guid Id, string Name, string? Description, bool IsActive,
            Guid OwnerUserId, string OwnerName, Guid CreatedBy, string CreatorName,
            int MembersCount, int ProjectsCount, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);



        [Fact]
        public async Task GetTeam_AsOwner_ReturnsTeamDetailsWithCounts()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("owner");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("viewer");

            var teamId = await _helpers.CreateTeamAsync("Detail Team", "Detail Description", ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                db.Projects.Add(new Project(Guid.NewGuid(), teamId, "Seed Project", ownerId));
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _helpers.GetTeamAsync(teamId, ownerToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var teamDto = await response.Content.ReadFromJsonAsync<GetTeamResponseDto>();

            Assert.Equal("Detail Team", teamDto!.Name);
            Assert.Equal("Detail Description", teamDto.Description);
            Assert.True(teamDto.IsActive);
            Assert.Equal(ownerId, teamDto.OwnerUserId);
            Assert.Equal(2, teamDto.MembersCount);
            Assert.Equal(1, teamDto.ProjectsCount);
        }

        [Fact]
        public async Task GetTeam_AsMember_Returns200()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var teamId = await _helpers.CreateTeamAsync("Visible Team", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _helpers.GetTeamAsync(teamId, memberToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetTeam_AsNonMember_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();
            var (_, _, outsiderToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var teamId = await _helpers.CreateTeamAsync("Hidden Team", null, ownerToken);

            var response = await _helpers.GetTeamAsync(teamId, outsiderToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTeam_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();
            var teamId = await _helpers.CreateTeamAsync("No Token Team Get", null, ownerToken);

            var response = await _helpers.GetTeamAsync(teamId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTeam_WithUnknownTeamId_Returns403()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var response = await _helpers.GetTeamAsync(Guid.NewGuid(), accessToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTeam_OnDeletedTeam_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();
            var teamId = await _helpers.CreateTeamAsync("Gone Team", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _helpers.GetTeamAsync(teamId, ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
