using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamsController
{
    [Collection("Team")]
    public sealed class CreateTeamIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);

        private sealed record CreateTeamResponse(Guid Id);

        private static HttpRequestMessage CreateTeamRequest(string name, string? description, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Teams)
            {
                Content = JsonContent.Create(new { name, description })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return request;
        }

        [Fact]
        public async Task CreateTeam_ValidRequest_CreatesTeamWithOwnerMembership()
        {
            var (userId, _, accessToken) = await _helpers.CreateConfirmedUserAsync("creator");

            var response = await _client.SendAsync(CreateTeamRequest("My New Team", "A description", accessToken));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CreateTeamResponse>();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var team = await db.Teams.AsNoTracking().SingleAsync(t => t.Id == result!.Id);

            Assert.Equal("My New Team", team.Name);
            Assert.Equal("A description", team.Description);
            Assert.Equal(userId, team.OwnerUserId);
            Assert.Equal(userId, team.CreatedBy);
            Assert.True(team.IsActive);
            Assert.Null(team.DeletedAtUtc);

            var ownerMembership = await db.TeamMembers.AsNoTracking().SingleAsync(m => m.TeamId == team.Id);

            Assert.Equal(userId, ownerMembership.UserId);
            Assert.Equal(TeamRole.Owner, ownerMembership.TeamRole);
            Assert.Equal(TeamMemberStatus.Active, ownerMembership.Status);
        }

        [Fact]
        public async Task CreateTeam_WithoutAccessToken_Returns401()
        {
            var response = await _client.SendAsync(CreateTeamRequest("No Token Team", null));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateTeam_WithUnconfirmedEmail_Returns403()
        {
            var (_, email) = await _helpers.RegisterUserAsync("unconfirmed");
            var login = await _helpers.LoginAsync(email);

            var response = await _client.SendAsync(CreateTeamRequest("Unconfirmed Team", null, login.AccessToken));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.False(await db.Teams.AnyAsync(t => t.Name == "Unconfirmed Team"));
        }

        [Fact]
        public async Task CreateTeam_WithDuplicateName_Returns409()
        {
            var (_, _, accessToken) = await _helpers.CreateConfirmedUserAsync("duplicate");

            var first = await _client.SendAsync(CreateTeamRequest("Duplicate Team", null, accessToken));

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await _client.SendAsync(CreateTeamRequest("Duplicate Team", null, accessToken));

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(1, await db.Teams.CountAsync(t => t.Name == "Duplicate Team"));
        }

        [Fact]
        public async Task CreateTeam_NameComparison_IsCaseInsensitive()
        {
            var (_, _, accessToken) = await _helpers.CreateConfirmedUserAsync("casing");

            var first = await _client.SendAsync(CreateTeamRequest("Case Sensitive Team", null, accessToken));

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await _client.SendAsync(CreateTeamRequest("case sensitive team", null, accessToken));

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task CreateTeam_WithDeletedTeamName_AllowsReuse()
        {
            var (_, _, accessToken) = await _helpers.CreateConfirmedUserAsync("reuse");

            var first = await _client.SendAsync(CreateTeamRequest("Reusable Team", null, accessToken));

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            var firstResult = await first.Content.ReadFromJsonAsync<CreateTeamResponse>();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == firstResult!.Id);
                team.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var second = await _client.SendAsync(CreateTeamRequest("Reusable Team", "Recreated", accessToken));

            Assert.Equal(HttpStatusCode.Created, second.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(2, await db2.Teams.CountAsync(t => t.Name == "Reusable Team"));
            Assert.Equal(1, await db2.Teams.CountAsync(t => t.Name == "Reusable Team" && t.DeletedAtUtc == null));
        }

        [Fact]
        public async Task CreateTeam_WithInvalidPayload_Returns400WithFieldErrors()
        {
            var (_, _, accessToken) = await _helpers.CreateConfirmedUserAsync("invalid");

            var missingName = await _client.SendAsync(CreateTeamRequest("", null, accessToken));

            Assert.Equal(HttpStatusCode.BadRequest, missingName.StatusCode);

            var body = await missingName.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("Name", out _));

            var longDescription = await _client.SendAsync(CreateTeamRequest("Valid Name", new string('x', 501), accessToken));

            Assert.Equal(HttpStatusCode.BadRequest, longDescription.StatusCode);

            var descriptionBody = await longDescription.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(descriptionBody.TryGetProperty("errors", out var descriptionErrors));
            Assert.True(descriptionErrors.TryGetProperty("Description", out _));
        }

        [Fact]
        public async Task CreateTeam_ConcurrentDuplicates_ExactlyOneSucceeds()
        {
            var (_, _, accessToken) = await _helpers.CreateConfirmedUserAsync("concurrent");

            var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
                _client.SendAsync(CreateTeamRequest("Racing Team", null, accessToken))));

            var statuses = responses.Select(r => r.StatusCode).OrderBy(code => code).ToArray();

            Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], statuses);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(1, await db.Teams.CountAsync(t => t.Name == "Racing Team"));
        }
    }
}
