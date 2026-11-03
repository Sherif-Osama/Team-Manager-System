using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamsController
{
    [Collection("Team")]
    public sealed class GetTeamByNameIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new TeamTestHelpers(factory);
        private readonly AuthenticatedUser _authenticatedUser = new AuthenticatedUser(factory);
        private sealed record GetTeamByNameResponseDto(Guid Id, string Name, string? Description, bool IsActive,
            Guid OwnerUserId, Guid CreatedBy, DateTime CreatedAtUtc);

        [Fact]
        public async Task GetTeamByName_AsAdmin_ReturnsTeam()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("admin");
            var (ownerId, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var teamId = await _helpers.CreateTeamAsync("Findable Team", null, ownerToken);

            var response = await _helpers.GetByNameAsync("Findable Team", adminToken);

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
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var response = await _helpers.GetByNameAsync("Anything", accessToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamByName_WithoutAccessToken_Returns401()
        {
            var response = await _helpers.GetByNameAsync("Anything");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamByName_WithUnknownName_Returns404()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("admin");

            var response = await _helpers.GetByNameAsync($"missing-{Guid.NewGuid():N}", adminToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamByName_IsCaseInsensitive()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            await _helpers.CreateTeamAsync("Cased Team", null, ownerToken);

            var response = await _helpers.GetByNameAsync("cased team", adminToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamByName_OnDeletedTeam_Returns404()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var teamId = await _helpers.CreateTeamAsync("Deleted By Name", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == teamId);
                team.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _helpers.GetByNameAsync("Deleted By Name", adminToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTeamByName_WithEmptyName_Returns400WithFieldErrors()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("admin");

            var response = await _helpers.GetByNameAsync("", adminToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("Name", out _));
        }
    }
}
