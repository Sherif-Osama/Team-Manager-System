using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamMembers
{
    [Collection("Team")]
    public sealed class RemoveMemberIntegrationTests(TeamsFixture factory)
    {
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private sealed record AddMemberResponse(long Id);

        private async Task<(Guid TeamId, long MemberId)> CreateTeamWithMemberAsync(string ownerPrefix, string memberPrefix)
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync(ownerPrefix);
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync(memberPrefix);

            var teamId = await _helpers.CreateTeamAsync($"Remove Team {ownerPrefix}", null, ownerToken);
            var add = await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.Created, add.StatusCode);
            return (teamId, (await add.Content.ReadFromJsonAsync<AddMemberResponse>())!.Id);
        }

        [Fact]
        public async Task RemoveMember_AsOwner_RemovesMemberAndRevokesAccess()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("remove-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("remove-member");

            var teamId = await _helpers.CreateTeamAsync("Remove Team One", null, ownerToken);

            var add = await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);
            var memberRowId = (await add.Content.ReadFromJsonAsync<AddMemberResponse>())!.Id;

            var response = await _helpers.RemoveMemberAsync(teamId, memberRowId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var membership = await db.TeamMembers.AsNoTracking().SingleAsync(m => m.Id == memberRowId);

            Assert.Equal(TeamMemberStatus.Removed, membership.Status);
            Assert.NotNull(membership.RemovedAtUtc);
            Assert.Equal(ownerId, membership.RemovedBy);

            var teamAccess = await _helpers.GetTeamAsync(teamId, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, teamAccess.StatusCode);
        }

        [Fact]
        public async Task RemoveMember_AsAdmin_RemovesMember()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("remove-owner");
            var (adminId, _, adminToken) = await _authenticatedUser.CreateConfirmedUserAsync("remove-admin");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("remove-target");

            var teamId = await _helpers.CreateTeamAsync("Remove Team Two", null, ownerToken);

            await _helpers.AddMemberAsync(teamId, adminId, TeamRole.Admin, ownerToken);
            var add = await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);
            var memberRowId = (await add.Content.ReadFromJsonAsync<AddMemberResponse>())!.Id;

            var response = await _helpers.RemoveMemberAsync(teamId, memberRowId, adminToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task RemoveMember_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("remove-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("remove-member");
            var (targetId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("remove-target");

            var teamId = await _helpers.CreateTeamAsync("Remove Team Three", null, ownerToken);

            await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);
            var add = await _helpers.AddMemberAsync(teamId, targetId, TeamRole.Member, ownerToken);
            var targetRowId = (await add.Content.ReadFromJsonAsync<AddMemberResponse>())!.Id;

            var response = await _helpers.RemoveMemberAsync(teamId, targetRowId, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RemoveMember_WithoutAccessToken_Returns401()
        {
            var (teamId, memberId) = await CreateTeamWithMemberAsync("notoken-owner", "notoken-member");

            var response = await _helpers.RemoveMemberAsync(teamId, memberId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RemoveMember_OfOwner_Returns400()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("remove-owner");

            var teamId = await _helpers.CreateTeamAsync("Remove Team Four", null, ownerToken);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var ownerRowId = await db.TeamMembers.Where(m => m.TeamId == teamId && m.UserId == ownerId)
                .Select(m => m.Id).SingleAsync();

            var response = await _helpers.RemoveMemberAsync(teamId, ownerRowId, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RemoveMember_WithUnknownMemberId_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("remove-unknown");
            var teamId = await _helpers.CreateTeamAsync("Remove Team Five", null, ownerToken);

            var response = await _helpers.RemoveMemberAsync(teamId, 999999, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RemoveMember_WithInvalidPayload_Returns400WithFieldErrors()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("remove-invalid");
            var teamId = await _helpers.CreateTeamAsync("Remove Team Six", null, ownerToken);

            var response = await _helpers.RemoveMemberAsync(teamId, 0, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("MemberId", out _));
        }
    }
}
