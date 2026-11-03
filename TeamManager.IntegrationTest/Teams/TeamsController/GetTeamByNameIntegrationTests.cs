using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamsController
{
    [Collection("Team")]
    public sealed class GetTeamByNameIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);

        private sealed record CreateTeamResponse(Guid Id);

        private sealed record GetTeamByNameResponseDto(Guid Id, string Name, string? Description, bool IsActive,
            Guid OwnerUserId, Guid CreatedBy, DateTime CreatedAtUtc);

        private async Task<Guid> CreateTeamAsync(string name, string accessToken)
        {
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Teams)
            {
                Content = JsonContent.Create(new { name, description = (string?)null }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<CreateTeamResponse>())!.Id;
        }

        private async Task<HttpResponseMessage> GetByNameAsync(string name, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{TeamEndpoints.TeamsByName}?name={Uri.EscapeDataString(name)}");

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        private async Task<(Guid Id, string AccessToken)> CreateAdminAsync(string prefix)
        {
            var (userId, _, accessToken) = await _helpers.CreateConfirmedUserAsync(prefix);
            await _helpers.PromoteToAdminAsync(userId);
            return (userId, accessToken);
        }

        [Fact]
        public async Task GetTeamByName_AsAdmin_ReturnsTeam()
        {
            var (_, adminToken) = await CreateAdminAsync("admin");
            var (ownerId, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");

            var teamId = await CreateTeamAsync("Findable Team", ownerToken);

            var response = await GetByNameAsync("Findable Team", adminToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GetTeamByNameResponseDto>();

            Assert.Equal(teamId, result!.Id);
            Assert.Equal("Findable Team", result.Name);
            Assert.True(result.IsActive);
            Assert.Equal(ownerId, result.OwnerUserId);
        }

        [Fact]
        public async Task GetTeamByName_AsRegularUser_Returns403()
        {
            var (_, _, accessToken) = await _helpers.CreateConfirmedUserAsync("regular");

            var response = await GetByNameAsync("Anything", accessToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamByName_WithoutAccessToken_Returns401()
        {
            var response = await GetByNameAsync("Anything");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamByName_WithUnknownName_Returns404()
        {
            var (_, adminToken) = await CreateAdminAsync("admin");

            var response = await GetByNameAsync($"missing-{Guid.NewGuid():N}", adminToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamByName_IsCaseInsensitive()
        {
            var (_, adminToken) = await CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");

            await CreateTeamAsync("Cased Team", ownerToken);

            var response = await GetByNameAsync("cased team", adminToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamByName_OnDeletedTeam_Returns404()
        {
            var (_, adminToken) = await CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");

            var teamId = await CreateTeamAsync("Deleted By Name", ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await GetByNameAsync("Deleted By Name", adminToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamByName_WithEmptyName_Returns400WithFieldErrors()
        {
            var (_, adminToken) = await CreateAdminAsync("admin");

            var response = await GetByNameAsync("", adminToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("Name", out _));
        }
    }
}
