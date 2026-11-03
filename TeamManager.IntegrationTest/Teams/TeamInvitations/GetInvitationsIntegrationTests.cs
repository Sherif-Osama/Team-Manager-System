using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamInvitations
{
    [Collection("Team")]
    public sealed class GetInvitationsIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private sealed record InviteMemberResponse(Guid Id);

        private sealed record TeamInvitationItemDto(Guid Id, string TeamName, string InvitedEmail, Guid? InvitedUserId,
            Guid InvitedBy, TeamRole TeamRole, TeamInvitationStatus Status, DateTime ExpiresAtUtc);

        private sealed record GetInvitationsResponseDto(List<TeamInvitationItemDto> Items, int Page, int PageSize, int TotalCount);

        private async Task<(Guid TeamId, Guid InvitationId, string Email)> InviteAsync(string accessToken)
        {
            var teamId = await _helpers.CreateTeamAsync($"GetInv Team {Guid.NewGuid().ToString("N")[..6]}", null, accessToken);
            var email = $"invitee-{Guid.NewGuid():N}@example.com";

            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Invitations(teamId))
            {
                Content = JsonContent.Create(new { email, teamRole = TeamRole.Member }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (teamId, (await response.Content.ReadFromJsonAsync<InviteMemberResponse>())!.Id, email);
        }

        [Fact]
        public async Task GetInvitations_AsOwner_ReturnsPendingInvitations()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getinv-owner");
            var (teamId, invitationId, email) = await InviteAsync(ownerToken);

            var request = new HttpRequestMessage(HttpMethod.Get, TeamEndpoints.Invitations(teamId));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GetInvitationsResponseDto>();

            var invitation = Assert.Single(result!.Items, i => i.Id == invitationId);

            Assert.Equal(email, invitation.InvitedEmail);
            Assert.Equal(TeamInvitationStatus.Pending, invitation.Status);
            Assert.Null(invitation.InvitedUserId);
        }

        [Fact]
        public async Task GetInvitations_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getinv-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("getinv-member");

            var teamId = await _helpers.CreateTeamAsync($"GetInv Team {Guid.NewGuid().ToString("N")[..6]}", null, ownerToken);

            await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);

            var request = new HttpRequestMessage(HttpMethod.Get, TeamEndpoints.Invitations(teamId));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetMyInvitations_AsInvitee_ReturnsInvitation()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getinv-owner");
            var (_, invitationId, email) = await InviteAsync(ownerToken);

            var (inviteeId, _) = await _authenticatedUser.RegisterUserWithEmailAsync(email);
            await _authenticatedUser.ConfirmEmailAsync(inviteeId, email);
            var inviteeToken = (await _authenticatedUser.LoginAsync(email)).AccessToken;

            var request = new HttpRequestMessage(HttpMethod.Get, TeamEndpoints.MyInvitations);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", inviteeToken);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GetInvitationsResponseDto>();

            Assert.Contains(result!.Items, i => i.Id == invitationId && i.Status == TeamInvitationStatus.Pending);
        }

        [Fact]
        public async Task GetInvitations_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("getinv-notoken");
            var (teamId, _, _) = await InviteAsync(ownerToken);

            var response = await _client.GetAsync(TeamEndpoints.Invitations(teamId));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
