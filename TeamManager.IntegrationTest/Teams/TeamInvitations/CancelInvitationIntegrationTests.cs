using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.Team;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamInvitations
{
    [Collection("Teams")]
    public sealed class CancelInvitationIntegrationTests(TeamFactory factory) : IntegrationTestBase(factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new(factory);

        private sealed record InviteMemberResponse(Guid Id);

        private async Task<(Guid TeamId, Guid InvitationId)> InviteAsync(string accessToken)
        {
            var teamId = await _helpers.CreateTeamAsync($"Cancel Team {Guid.NewGuid().ToString("N")[..6]}", null, accessToken);
            var email = $"invitee-{Guid.NewGuid():N}@example.com";

            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Invitations(teamId))
            {
                Content = JsonContent.Create(new { email, teamRole = TeamRole.Member }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (teamId, (await response.Content.ReadFromJsonAsync<InviteMemberResponse>())!.Id);
        }

        private async Task<HttpResponseMessage> CancelAsync(Guid teamId, Guid invitationId, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, TeamEndpoints.Invitation(teamId, invitationId));

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task CancelInvitation_AsOwner_CancelsInvitation()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cancel-owner");
            var (teamId, invitationId) = await InviteAsync(ownerToken);

            var response = await CancelAsync(teamId, invitationId, ownerToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var invitation = await db.TeamInvitations.AsNoTracking().SingleAsync(i => i.Id == invitationId);

            Assert.Equal(TeamInvitationStatus.Cancelled, invitation.Status);
            Assert.NotNull(invitation.CancelledAtUtc);
        }

        [Fact]
        public async Task CancelInvitation_AsInvitingAdmin_CancelsInvitation()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cancel-owner");
            var (adminId, _, adminToken) = await _authenticatedUser.CreateConfirmedUserAsync("cancel-admin");

            var teamId = await _helpers.CreateTeamAsync($"Cancel Team {Guid.NewGuid().ToString("N")[..6]}", null, ownerToken);

            await _helpers.AddMemberAsync(teamId, adminId, TeamRole.Admin, ownerToken);

            var (inviteTeamId, invitationId) = await InviteAsync(adminToken);

            var response = await CancelAsync(inviteTeamId, invitationId, adminToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task CancelInvitation_AsNonInvitingAdmin_Returns400()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cancel-owner");
            var (inviterAdminId, _, inviterAdminToken) = await _authenticatedUser.CreateConfirmedUserAsync("cancel-inviter");
            var (otherAdminId, _, otherAdminToken) = await _authenticatedUser.CreateConfirmedUserAsync("cancel-other");

            var teamId = await _helpers.CreateTeamAsync($"Cancel Team {Guid.NewGuid().ToString("N")[..6]}", null, ownerToken);

            await _helpers.AddMemberAsync(teamId, inviterAdminId, TeamRole.Admin, ownerToken);
            await _helpers.AddMemberAsync(teamId, otherAdminId, TeamRole.Admin, ownerToken);

            var (_, invitationId) = await InviteAsync(inviterAdminToken);

            var response = await CancelAsync(teamId, invitationId, otherAdminToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CancelInvitation_AsMember_Returns403()
        {
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cancel-owner");
            var (memberId, _, memberToken) = await _authenticatedUser.CreateConfirmedUserAsync("cancel-member");

            var teamId = await _helpers.CreateTeamAsync($"Cancel Team {Guid.NewGuid().ToString("N")[..6]}", null, ownerToken);

            await _helpers.AddMemberAsync(teamId, memberId, TeamRole.Member, ownerToken);

            var (_, invitationId) = await InviteAsync(ownerToken);

            var response = await CancelAsync(teamId, invitationId, memberToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CancelInvitation_WithUnknownInvitationId_Returns400()
        {
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync("cancel-unknown");
            var teamId = await _helpers.CreateTeamAsync($"Cancel Team {Guid.NewGuid().ToString("N")[..6]}", null, ownerToken);

            var response = await CancelAsync(teamId, Guid.NewGuid(), ownerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
