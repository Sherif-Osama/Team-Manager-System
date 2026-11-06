using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamMembers
{
    [Collection("Team")]
    public sealed class GetMembersIntegrationTests(TeamsFixture factory)
    {
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private sealed record AddMemberResponse(long Id);

        private sealed record TeamMemberItemDto(long Id, Guid UserId, string DisplayName, string Email,
            TeamRole TeamRole, TeamMemberStatus Status, DateTime JoinedAtUtc);

        private sealed record GetMembersResponseDto(List<TeamMemberItemDto> Items, int Page, int PageSize, int TotalCount);

        private sealed record GetMemberResponseDto(long Id, Guid UserId, string DisplayName, string Email,
            TeamRole TeamRole, TeamMemberStatus Status, DateTime JoinedAtUtc, Guid? InvitedBy, string? InvitedByName,
            DateTime? RemovedAtUtc, Guid? RemovedBy, string? RemovedByName);

        [Fact]
        public async Task GetMembers_AsOwner_ReturnsAllMembers()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("members-owner");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("members-viewer");

            var teamId = await _helpers.CreateTeamAsync("Members Team One", null, ownerToken);

            var add = await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.Created, add.StatusCode);

            var response = await _helpers.GetMembersAsync(teamId, ownerToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GetMembersResponseDto>();

            Assert.Equal(2, result!.TotalCount);
            Assert.Contains(result.Items, i => i.UserId == ownerId && i.TeamRole == TeamRole.Owner);
            Assert.Contains(result.Items, i => i.UserId == memberId && i.TeamRole == TeamRole.Member && i.Status == TeamMemberStatus.Active);
        }

        [Fact]
        public async Task GetMembers_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("members-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("members-member");

            var teamId = await _helpers.CreateTeamAsync("Members Team Two", null, ownerToken);

            await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);

            var response = await _helpers.GetMembersAsync(teamId, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetMembers_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("members-notoken");
            var teamId = await _helpers.CreateTeamAsync("Members Team Three", null, ownerToken);

            var response = await _helpers.GetMembersAsync(teamId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetMember_AsOwner_ReturnsMemberDetail()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("member-owner");
            var (memberId, _, _) = await _authenticatedUser.CreateConfirmedUserAsync("member-target");

            var teamId = await _helpers.CreateTeamAsync("Members Team Four", null, ownerToken);

            var add = await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);
            var memberRowId = (await add.Content.ReadFromJsonAsync<AddMemberResponse>())!.Id;

            var response = await _helpers.GetMemberAsync(teamId, memberRowId, ownerToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GetMemberResponseDto>();

            Assert.Equal(memberRowId, result!.Id);
            Assert.Equal(memberId, result.UserId);
            Assert.Equal(TeamRole.Member, result.TeamRole);
            Assert.Equal(TeamMemberStatus.Active, result.Status);
        }

    }
}
