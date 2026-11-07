using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamMembers
{
    [Collection("Common")]
    public sealed class AddMemberIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private sealed record AddMemberResponse(long Id);

        [Fact]
        public async Task AddMember_AsOwner_AddsActiveMemberWithImmediateAccess()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-member");

            var teamId = await _helpers.CreateTeamAsync("Add Team One", null, ownerToken);

            var response = await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<AddMemberResponse>();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var membership = await db.TeamMembers.AsNoTracking().SingleAsync(m => m.Id == result!.Id);

            Assert.Equal(teamId, membership.TeamId);
            Assert.Equal(memberId, membership.UserId);
            Assert.Equal(TeamRole.Member, membership.TeamRole);
            Assert.Equal(TeamMemberStatus.Active, membership.Status);

            var teamAccess = await _helpers.GetTeamAsync(teamId, memberToken);

            Assert.Equal(HttpStatusCode.OK, teamAccess.StatusCode);
        }

        [Fact]
        public async Task AddMember_AsAdmin_AddsMember()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-owner");
            var (adminId, _, adminToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-admin");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("add-target");

            var teamId = await _helpers.CreateTeamAsync("Add Team Two", null, ownerToken);

            var promote = await _helpers.AddMemberAsync(teamId, adminId, TeamRole.Admin, ownerToken);

            Assert.Equal(HttpStatusCode.Created, promote.StatusCode);

            var response = await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, adminToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task AddMember_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-member");
            var (targetId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("add-target");

            var teamId = await _helpers.CreateTeamAsync("Add Team Three", null, ownerToken);

            await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);

            var response = await _helpers.AddMemberAsync(teamId, targetId, TeamRole.Member, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddMember_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-notoken");
            var teamId = await _helpers.CreateTeamAsync("Add Team Four", null, ownerToken);

            var response = await _helpers.AddMemberAsync(teamId, Guid.NewGuid(), TeamRole.Member);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddMember_WithUnknownUser_Returns404()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-unknown");
            var teamId = await _helpers.CreateTeamAsync("Add Team Five", null, ownerToken);

            var response = await _helpers.AddMemberAsync(teamId, Guid.NewGuid(), TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddMember_WithInactiveUser_Returns404()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-owner");
            var (targetId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("add-inactive");

            var teamId = await _helpers.CreateTeamAsync("Add Team Six", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var target = await db.Users.SingleAsync(u => u.Id == targetId);
                target.Deactivate();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _helpers.AddMemberAsync(teamId, targetId, TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddMember_WithOwnerRole_Returns400()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-ownerrole");
            var (targetId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("add-target");

            var teamId = await _helpers.CreateTeamAsync("Add Team Seven", null, ownerToken);

            var response = await _helpers.AddMemberAsync(teamId, targetId, TeamRole.Owner, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddMember_WithDuplicateMembership_Returns400()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-dup");
            var (targetId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("add-target");

            var teamId = await _helpers.CreateTeamAsync("Add Team Eight", null, ownerToken);

            var first = await _helpers.AddMemberAsync(teamId, targetId, TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await _helpers.AddMemberAsync(teamId, targetId, TeamRole.Admin, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task AddMember_ConcurrentDuplicates_ExactlyOneSucceeds()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("add-race");
            var (targetId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("add-target");

            var teamId = await _helpers.CreateTeamAsync("Add Team Nine", null, ownerToken);

            var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
                _helpers.AddMemberAsync(teamId, targetId, TeamRole.Member, ownerToken)));

            var statuses = responses.Select(r => r.StatusCode).OrderBy(code => code).ToArray();

            Assert.Equal([HttpStatusCode.Created, HttpStatusCode.BadRequest], statuses);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(1, await db.TeamMembers.CountAsync(m => m.TeamId == teamId && m.UserId == targetId));
        }
    }
}
