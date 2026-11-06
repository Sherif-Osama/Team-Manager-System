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
    public sealed class AcceptInvitationIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private sealed record InviteMemberResponse(Guid Id);

        private async Task<(Guid TeamId, Guid InvitationId, string Token, string Email)> InviteAsync(string ownerToken)
        {
            var teamId = await _helpers.CreateTeamAsync($"Accept Team {Guid.NewGuid().ToString("N")[..6]}", null, ownerToken);
            var email = $"invitee-{Guid.NewGuid():N}@example.com";

            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Invitations(teamId))
            {
                Content = JsonContent.Create(new { email, teamRole = TeamRole.Member }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", ownerToken) }
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var invitationId = (await response.Content.ReadFromJsonAsync<InviteMemberResponse>())!.Id;

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var outboxMessage = await db.OutboxMessages.AsNoTracking()
                .SingleAsync(o => o.Type == "InvitationEmail" && o.Payload.Contains(email));

            using var payload = JsonDocument.Parse(outboxMessage.Payload);
            var token = payload.RootElement.GetProperty("Token").GetString()!;

            return (teamId, invitationId, token, email);
        }

        private async Task<HttpResponseMessage> AcceptAsync(string token, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.AcceptInvitation)
            {
                Content = JsonContent.Create(new { token })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task AcceptInvitation_AsInvitee_CreatesMembershipWithImmediateAccess()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("accept-owner");
            var (teamId, invitationId, token, email) = await InviteAsync(ownerToken);

            var (inviteeId, _) = await _authenticatedUser.RegisterUserWithEmailAsync(email);
            await _authenticatedUser.ConfirmEmailAsync(inviteeId, email);
            var login = await _authenticatedUser.LoginAsync(email);

            var response = await AcceptAsync(token, login.AccessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var invitation = await db.TeamInvitations.AsNoTracking().SingleAsync(i => i.Id == invitationId);

            Assert.Equal(TeamInvitationStatus.Accepted, invitation.Status);
            Assert.NotNull(invitation.AcceptedAtUtc);

            var membership = await db.TeamMembers.AsNoTracking()
                .SingleAsync(m => m.TeamId == teamId && m.UserId == inviteeId);

            Assert.Equal(TeamRole.Member, membership.TeamRole);
            Assert.Equal(TeamMemberStatus.Active, membership.Status);

            var teamAccess = await _helpers.GetTeamAsync(teamId, login.AccessToken);

            Assert.Equal(HttpStatusCode.OK, teamAccess.StatusCode);
        }

        [Fact]
        public async Task AcceptInvitation_WithoutAccessToken_Returns401()
        {
            var response = await AcceptAsync("some-token");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AcceptInvitation_WithUnknownToken_Returns404()
        {
            var (_, _, inviteeToken) = await _authenticatedUser.CreateConfirmedUserAsync("accept-unknown");

            var response = await AcceptAsync("not-a-real-token", inviteeToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AcceptInvitation_ByWrongEmail_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("accept-owner");
            var (_, _, token, _) = await InviteAsync(ownerToken);

            var (_, _, wrongUserToken) = await _authenticatedUser.CreateConfirmedUserAsync("accept-wrongemail");

            var response = await AcceptAsync(token, wrongUserToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AcceptInvitation_AlreadyAccepted_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("accept-owner");
            var (_, _, token, email) = await InviteAsync(ownerToken);

            var (inviteeId, _) = await _authenticatedUser.RegisterUserWithEmailAsync(email);
            await _authenticatedUser.ConfirmEmailAsync(inviteeId, email);
            var login = await _authenticatedUser.LoginAsync(email);

            var first = await AcceptAsync(token, login.AccessToken);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

            var second = await AcceptAsync(token, login.AccessToken);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task AcceptInvitation_ExpiredInvitation_Returns400AndMarksExpired()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("accept-owner");
            var (_, invitationId, token, email) = await InviteAsync(ownerToken);

            var (inviteeId, _) = await _authenticatedUser.RegisterUserWithEmailAsync(email);
            await _authenticatedUser.ConfirmEmailAsync(inviteeId, email);
            var login = await _authenticatedUser.LoginAsync(email);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                await db.TeamInvitations.Where(i => i.Id == invitationId).ExecuteUpdateAsync(i => i.SetProperty(i => i.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-5)));
            }

            var response = await AcceptAsync(token, login.AccessToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var invitation = await db2.TeamInvitations.AsNoTracking().SingleAsync(i => i.Id == invitationId);

            Assert.Equal(TeamInvitationStatus.Expired, invitation.Status);
        }

    }
}
