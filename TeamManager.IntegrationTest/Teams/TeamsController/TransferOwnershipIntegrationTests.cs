using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamsController
{
    [Collection("Common")]
    public sealed class TransferOwnershipIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private async Task<HttpResponseMessage> TransferAsync(Guid teamId, Guid newOwnerUserId, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId))
            {
                Content = JsonContent.Create(new { newOwnerUserId })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task TransferOwnership_AsOwner_TransfersToActiveMember()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-owner");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-member");

            var teamId = await _helpers.CreateTeamAsync("Transfer Team One", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await TransferAsync(teamId, memberId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var team2 = await db2.Teams.AsNoTracking().SingleAsync(t => t.Id == teamId);

            Assert.Equal(memberId, team2.OwnerUserId);

            var ownerMembership = await db2.TeamMembers.AsNoTracking().SingleAsync(m => m.TeamId == teamId && m.UserId == ownerId);
            var newOwnerMembership = await db2.TeamMembers.AsNoTracking().SingleAsync(m => m.TeamId == teamId && m.UserId == memberId);

            Assert.Equal(TeamRole.Admin, ownerMembership.TeamRole);
            Assert.Equal(TeamRole.Owner, newOwnerMembership.TeamRole);
        }

        [Fact]
        public async Task TransferOwnership_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-member");

            var teamId = await _helpers.CreateTeamAsync("Transfer Team Two", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await TransferAsync(teamId, ownerId, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-notoken");
            var teamId = await _helpers.CreateTeamAsync("Transfer Team Three", null, ownerToken);

            var response = await TransferAsync(teamId, Guid.NewGuid());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_ToUnknownUser_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-unknown");
            var teamId = await _helpers.CreateTeamAsync("Transfer Team Four", null, ownerToken);

            var response = await TransferAsync(teamId, Guid.NewGuid(), ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_ToInactiveUser_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-inactive");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-target");

            var teamId = await _helpers.CreateTeamAsync("Transfer Team Five", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.AddMember(memberId, TeamRole.Member);
                var targetUser = await db.Users.SingleAsync(u => u.Id == memberId);
                targetUser.Deactivate();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await TransferAsync(teamId, memberId, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_ToNonMemberUser_Returns400()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-owner");
            var (outsiderId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-outsider");

            var teamId = await _helpers.CreateTeamAsync("Transfer Team Six", null, ownerToken);

            var response = await TransferAsync(teamId, outsiderId, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TransferOwnership_ToSelf_Returns400()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-self");
            var teamId = await _helpers.CreateTeamAsync("Transfer Team Seven", null, ownerToken);

            var response = await TransferAsync(teamId, ownerId, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }


        [Fact]
        public async Task TransferOwnership_OnDeletedTeam_Returns403()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("transfer-deleted");
            var teamId = await _helpers.CreateTeamAsync("Transfer Team Nine", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await TransferAsync(teamId, Guid.NewGuid(), ownerToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
