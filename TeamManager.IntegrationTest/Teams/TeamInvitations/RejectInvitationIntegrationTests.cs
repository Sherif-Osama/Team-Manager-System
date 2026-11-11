using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Team;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamInvitations
{
    [Collection("Teams")]
    public sealed class RejectInvitationIntegrationTests(TeamFactory factory) : IntegrationTestBase(factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private sealed record InviteMemberResponse(Guid Id);

        private async Task<(Guid InvitationId, string Token, string Email)> InviteAsync(string ownerToken)
        {
            var teamId = await _helpers.CreateTeamAsync($"Reject Team {Guid.NewGuid().ToString("N")[..6]}", null, ownerToken);
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

            return (invitationId, token, email);
        }

        private async Task<HttpResponseMessage> RejectAsync(string token, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.RejectInvitation)
            {
                Content = JsonContent.Create(new { token })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        private async Task<(string AccessToken, Guid UserId)> LoginInviteeAsync(string email)
        {
            var (inviteeId, _) = await _authenticatedUser.RegisterUserWithEmailAsync(email);
            await _authenticatedUser.ConfirmEmailAsync(inviteeId, email);
            return ((await _authenticatedUser.LoginAsync(email)).AccessToken, inviteeId);
        }

        [Fact]
        public async Task RejectInvitation_AsInvitee_RejectsInvitation()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("reject-owner");
            var (invitationId, token, email) = await InviteAsync(ownerToken);

            var (inviteeToken, inviteeId) = await LoginInviteeAsync(email);

            var response = await RejectAsync(token, inviteeToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var invitation = await db.TeamInvitations.AsNoTracking().SingleAsync(i => i.Id == invitationId);

            Assert.Equal(TeamInvitationStatus.Rejected, invitation.Status);
            Assert.NotNull(invitation.RejectedAtUtc);

            Assert.False(await db.TeamMembers.AnyAsync(m => m.TeamId == invitation.TeamId && m.UserId == inviteeId));
        }

        [Fact]
        public async Task RejectInvitation_WithUnknownToken_Returns404()
        {
            var (_, _, inviteeToken) = await _authenticatedUser.CreateConfirmedUserAsync("reject-unknown");

            var response = await RejectAsync("not-a-real-token", inviteeToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RejectInvitation_ByWrongEmail_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("reject-owner");
            var (_, token, _) = await InviteAsync(ownerToken);

            var (_, _, wrongUserToken) = await _authenticatedUser.CreateConfirmedUserAsync("reject-wrongemail");

            var response = await RejectAsync(token, wrongUserToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RejectInvitation_AlreadyRejected_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("reject-owner");
            var (_, token, email) = await InviteAsync(ownerToken);

            var (inviteeToken, _) = await LoginInviteeAsync(email);

            var first = await RejectAsync(token, inviteeToken);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

            var second = await RejectAsync(token, inviteeToken);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }
    }
}
