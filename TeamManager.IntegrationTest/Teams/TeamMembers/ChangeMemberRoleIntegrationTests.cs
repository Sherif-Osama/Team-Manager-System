using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamMembers
{
    [Collection("Team")]
    public sealed class ChangeMemberRoleIntegrationTests(TeamsFixture factory)
    {
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private sealed record AddMemberResponse(long Id);

        private async Task<(Guid TeamId, long MemberId)> AddMemberToTeamAsync(Guid teamId, Guid userId, string ownerToken, TeamRole role = TeamRole.Member)
        {
            var add = await _helpers.AddMemberAsync(teamId, userId, role, ownerToken);

            Assert.Equal(HttpStatusCode.Created, add.StatusCode);
            return (teamId, (await add.Content.ReadFromJsonAsync<AddMemberResponse>())!.Id);
        }

        [Fact]
        public async Task ChangeMemberRole_AsOwner_PromotesMemberToAdminWithImmediateEffect()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("role-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("role-member");

            var teamId = await _helpers.CreateTeamAsync("Role Team One", null, ownerToken);
            var (_, memberRowId) = await AddMemberToTeamAsync(teamId, memberId, ownerToken);

            var response = await _helpers.ChangeMemberRoleAsync(teamId, memberRowId, TeamRole.Admin, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var membership = await db.TeamMembers.AsNoTracking().SingleAsync(m => m.Id == memberRowId);

            Assert.Equal(TeamRole.Admin, membership.TeamRole);

            var membersList = await _helpers.GetMembersAsync(teamId, memberToken);

            Assert.Equal(HttpStatusCode.OK, membersList.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_AsAdmin_ChangesRole()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("role-owner");
            var (adminId, _, adminToken) = await _authenticatedUser.CreateConfirmedUserAsync("role-admin");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("role-target");

            var teamId = await _helpers.CreateTeamAsync("Role Team Two", null, ownerToken);

            await AddMemberToTeamAsync(teamId, adminId, ownerToken, TeamRole.Admin);
            var (_, memberRowId) = await AddMemberToTeamAsync(teamId, memberId, ownerToken);

            var response = await _helpers.ChangeMemberRoleAsync(teamId, memberRowId, TeamRole.Admin, adminToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(TeamRole.Admin, await db.TeamMembers.AsNoTracking()
                .Where(m => m.Id == memberRowId).Select(m => m.TeamRole).SingleAsync());
        }

        [Fact]
        public async Task ChangeMemberRole_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("role-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("role-member");
            var (targetId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("role-target");

            var teamId = await _helpers.CreateTeamAsync("Role Team Three", null, ownerToken);

            await AddMemberToTeamAsync(teamId, memberId, ownerToken);
            var (_, targetRowId) = await AddMemberToTeamAsync(teamId, targetId, ownerToken);

            var response = await _helpers.ChangeMemberRoleAsync(teamId, targetRowId, TeamRole.Admin, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_OfOwner_Returns400()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("role-owner");

            var teamId = await _helpers.CreateTeamAsync("Role Team Four", null, ownerToken);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var ownerRowId = await db.TeamMembers.Where(m => m.TeamId == teamId && m.UserId == ownerId)
                .Select(m => m.Id).SingleAsync();

            var response = await _helpers.ChangeMemberRoleAsync(teamId, ownerRowId, TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_ToOwnerRole_Returns400()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("role-owner");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("role-target");

            var teamId = await _helpers.CreateTeamAsync("Role Team Five", null, ownerToken);
            var (_, memberRowId) = await AddMemberToTeamAsync(teamId, memberId, ownerToken);

            var response = await _helpers.ChangeMemberRoleAsync(teamId, memberRowId, TeamRole.Owner, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangeMemberRole_WithUnknownMemberId_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("role-unknown");
            var teamId = await _helpers.CreateTeamAsync("Role Team Six", null, ownerToken);

            var response = await _helpers.ChangeMemberRoleAsync(teamId, 999999, TeamRole.Admin, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
