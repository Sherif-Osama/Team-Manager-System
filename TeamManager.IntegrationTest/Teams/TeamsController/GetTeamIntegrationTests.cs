using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamsController
{
    [Collection("Team")]
    public sealed class GetTeamIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);

        private sealed record CreateTeamResponse(Guid Id);

        private sealed record GetTeamResponseDto(Guid Id, string Name, string? Description, bool IsActive,
            Guid OwnerUserId, string OwnerName, Guid CreatedBy, string CreatorName,
            int MembersCount, int ProjectsCount, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

        private async Task<Guid> CreateTeamAsync(string name, string accessToken, string? description = null)
        {
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Teams)
            {
                Content = JsonContent.Create(new { name, description }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<CreateTeamResponse>())!.Id;
        }

        private async Task<HttpResponseMessage> GetTeamAsync(Guid teamId, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, TeamEndpoints.Team(teamId));

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task GetTeam_AsOwner_ReturnsTeamDetailsWithCounts()
        {
            var (ownerId, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");
            var (memberId, _, _) = await _helpers.CreateConfirmedUserAsync("viewer");

            var teamId = await CreateTeamAsync("Detail Team", ownerToken, "Detail Description");

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                db.Projects.Add(new Project(Guid.NewGuid(), teamId, "Seed Project", ownerId));
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await GetTeamAsync(teamId, ownerToken);

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
            var (_, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");
            var (memberId, _, memberToken) = await _helpers.CreateConfirmedUserAsync("viewer");

            var teamId = await CreateTeamAsync("Visible Team", ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await GetTeamAsync(teamId, memberToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetTeam_AsNonMember_Returns403()
        {
            var (_, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");
            var (_, _, outsiderToken) = await _helpers.CreateConfirmedUserAsync("outsider");

            var teamId = await CreateTeamAsync("Hidden Team", ownerToken);

            var response = await GetTeamAsync(teamId, outsiderToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTeam_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");
            var teamId = await CreateTeamAsync("No Token Team Get", ownerToken);

            var response = await GetTeamAsync(teamId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTeam_WithUnknownTeamId_Returns403()
        {
            var (_, _, accessToken) = await _helpers.CreateConfirmedUserAsync("unknown");

            var response = await GetTeamAsync(Guid.NewGuid(), accessToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTeam_OnDeletedTeam_Returns403()
        {
            var (_, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("deleted");
            var teamId = await CreateTeamAsync("Gone Team", ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await GetTeamAsync(teamId, ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
