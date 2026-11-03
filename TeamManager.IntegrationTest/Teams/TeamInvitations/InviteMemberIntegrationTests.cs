using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamInvitations
{
    [Collection("Team")]
    public sealed class InviteMemberIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private sealed record InviteMemberResponse(Guid Id);

        private async Task<HttpResponseMessage> InviteAsync(Guid teamId, string email, TeamRole role, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Invitations(teamId))
            {
                Content = JsonContent.Create(new { email, teamRole = role })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task InviteMember_AsOwner_InvitesByEmailAndQueuesEmail()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("invite-owner");
            var teamId = await _helpers.CreateTeamAsync("Invite Team One", null, ownerToken);

            var targetEmail = $"invitee-{Guid.NewGuid():N}@example.com";

            var response = await InviteAsync(teamId, targetEmail, TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<InviteMemberResponse>();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var invitation = await db.TeamInvitations.AsNoTracking().SingleAsync(i => i.Id == result!.Id);

            Assert.Equal(teamId, invitation.TeamId);
            Assert.Equal(targetEmail, invitation.InvitedEmail);
            Assert.Null(invitation.InvitedUserId);
            Assert.Equal(ownerId, invitation.InvitedBy);
            Assert.Equal(TeamRole.Member, invitation.TeamRole);
            Assert.Equal(TeamInvitationStatus.Pending, invitation.Status);
            Assert.NotNull(invitation.TokenHash);

            var outboxMessage = await db.OutboxMessages.AsNoTracking()
                .SingleAsync(o => o.Type == "InvitationEmail" && o.Payload.Contains(targetEmail));

            using var payload = JsonDocument.Parse(outboxMessage.Payload);
            Assert.Equal(targetEmail, payload.RootElement.GetProperty("To").GetString());
            Assert.False(string.IsNullOrWhiteSpace(payload.RootElement.GetProperty("Token").GetString()));
        }

        [Fact]
        public async Task InviteMember_AsAdmin_Invites()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("invite-owner");
            var (adminId, _, adminToken) = await _authenticatedUser.CreateConfirmedUserAsync("invite-admin");

            var teamId = await _helpers.CreateTeamAsync("Invite Team Two", null, ownerToken);

            await _helpers.AddMemberAsync(teamId, adminId, TeamRole.Admin, ownerToken);

            var response = await InviteAsync(teamId, $"invitee-{Guid.NewGuid():N}@example.com", TeamRole.Member, adminToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task InviteMember_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("invite-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("invite-member");

            var teamId = await _helpers.CreateTeamAsync("Invite Team Three", null, ownerToken);

            await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);

            var response = await InviteAsync(teamId, "someone@example.com", TeamRole.Member, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task InviteMember_WithoutAccessToken_Returns401()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("invite-notoken");
            var teamId = await _helpers.CreateTeamAsync("Invite Team Four", null, ownerToken);

            var response = await InviteAsync(teamId, "someone@example.com", TeamRole.Member);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task InviteMember_WithUnconfirmedEmail_Returns403()
        {
            var (_, email) = await _authenticatedUser.RegisterUserAsync("invite-unconfirmed");
            var login = await _authenticatedUser.LoginAsync(email);

            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("invite-owner");
            var teamId = await _helpers.CreateTeamAsync("Invite Team Five", null, ownerToken);

            var response = await InviteAsync(teamId, "someone@example.com", TeamRole.Member, login.AccessToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task InviteMember_WithDuplicatePendingInvitation_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("invite-dup");
            var teamId = await _helpers.CreateTeamAsync("Invite Team Six", null, ownerToken);

            var targetEmail = $"invitee-{Guid.NewGuid():N}@example.com";

            var first = await InviteAsync(teamId, targetEmail, TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await InviteAsync(teamId, targetEmail.ToUpperInvariant(), TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task InviteMember_WithOwnerRole_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("invite-ownerrole");
            var teamId = await _helpers.CreateTeamAsync("Invite Team Seven", null, ownerToken);

            var response = await InviteAsync(teamId, $"invitee-{Guid.NewGuid():N}@example.com", TeamRole.Owner, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task InviteMember_WithInvalidPayload_Returns400WithFieldErrors()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("invite-invalid");
            var teamId = await _helpers.CreateTeamAsync("Invite Team Eight", null, ownerToken);

            var response = await InviteAsync(teamId, "", TeamRole.Member, ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("Email", out _));
        }
    }
}
