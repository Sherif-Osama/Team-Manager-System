using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Common;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Teams.TeamsController
{
    [Collection("Team")]
    public sealed class GetTeamsIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);
        private readonly AuthenticatedUser _authenticatedUser = new AuthenticatedUser(factory);

        private sealed record TeamListItemDto(Guid Id, string Name, string? Description, Guid OwnerUserId,
            string OwnerDisplayName, Guid CreatedBy, string CreatorDisplayName, DateTime CreatedAtUtc);

        private sealed record GetTeamsResponseDto(IReadOnlyCollection<TeamListItemDto> Items, int TotalCount, int Page, int PageSize);

        private async Task<HttpResponseMessage> GetTeamsAsync(string accessToken, string query = "")
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TeamEndpoints.Teams}{query}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task GetTeams_AsAdmin_ReturnsPagedTeams()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var firstId = await _helpers.CreateTeamAsync("First Team", null, ownerToken);
            var secondId = await _helpers.CreateTeamAsync("Second Team", null, ownerToken);

            var response = await GetTeamsAsync(adminToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GetTeamsResponseDto>();

            Assert.True(result!.TotalCount >= 2);
            Assert.Contains(result.Items, i => i.Id == firstId);
            Assert.Contains(result.Items, i => i.Id == secondId);
            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);
        }

        [Fact]
        public async Task GetTeams_AsRegularUser_Returns403()
        {
            var (_, _, accessToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var response = await GetTeamsAsync(accessToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetTeams_WithoutAccessToken_Returns401()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, TeamEndpoints.Teams);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTeams_WithSearch_FiltersByNameAndDescription()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            await _helpers.CreateTeamAsync("Alpha Team", null, ownerToken);
            await _helpers.CreateTeamAsync("Beta Team", "gamma project description", ownerToken);

            var byName = await GetTeamsAsync(adminToken, "?search=alpha");
            var byNameResult = await byName.Content.ReadFromJsonAsync<GetTeamsResponseDto>();

            Assert.Equal(1, byNameResult!.TotalCount);
            Assert.Equal("Alpha Team", byNameResult.Items.Single().Name);

            var byDescription = await GetTeamsAsync(adminToken, "?search=gamma");
            var byDescriptionResult = await byDescription.Content.ReadFromJsonAsync<GetTeamsResponseDto>();

            Assert.Equal(1, byDescriptionResult!.TotalCount);
            Assert.Equal("Beta Team", byDescriptionResult.Items.Single().Name);
        }

        [Fact]
        public async Task GetTeams_WithIsActiveFilter_ReturnsOnlyActiveTeams()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var activeId = await _helpers.CreateTeamAsync("Active Team", null, ownerToken);
            var inactiveId = await _helpers.CreateTeamAsync("Inactive Team", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == inactiveId);
                team.Deactivate();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await GetTeamsAsync(adminToken, "?isActive=true");
            var result = await response.Content.ReadFromJsonAsync<GetTeamsResponseDto>();

            Assert.Contains(result!.Items, i => i.Id == activeId);
            Assert.DoesNotContain(result.Items, i => i.Id == inactiveId);

            var inactiveOnly = await GetTeamsAsync(adminToken, "?isActive=false");
            var inactiveResult = await inactiveOnly.Content.ReadFromJsonAsync<GetTeamsResponseDto>();

            Assert.Contains(inactiveResult!.Items, i => i.Id == inactiveId);
            Assert.DoesNotContain(inactiveResult.Items, i => i.Id == activeId);
        }

        [Fact]
        public async Task GetTeams_WithPagination_ReturnsRequestedPage()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            await _helpers.CreateTeamAsync("Page One Team", null, ownerToken);
            await _helpers.CreateTeamAsync("Page Two Team", null, ownerToken);
            await _helpers.CreateTeamAsync("Page Three Team", null, ownerToken);

            var response = await GetTeamsAsync(adminToken, "?page=2&pageSize=2");
            var result = await response.Content.ReadFromJsonAsync<GetTeamsResponseDto>();

            Assert.Equal(2, result!.Page);
            Assert.Equal(2, result.PageSize);
            Assert.True(result.TotalCount >= 3);
            Assert.True(result?.Items.Count >= 1);
        }

        [Fact]
        public async Task GetTeams_ExcludesDeletedTeams()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _authenticatedUser.CreateConfirmedUserAsync();

            var aliveId = await _helpers.CreateTeamAsync("Alive Team", null, ownerToken);
            var deletedId = await _helpers.CreateTeamAsync("Removed Team", null, ownerToken);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var team = await db.Teams.SingleAsync(t => t.Id == deletedId);
                team.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await GetTeamsAsync(adminToken);
            var result = await response.Content.ReadFromJsonAsync<GetTeamsResponseDto>();

            Assert.Contains(result!.Items, i => i.Id == aliveId);
            Assert.DoesNotContain(result.Items, i => i.Id == deletedId);
        }

        [Fact]
        public async Task GetTeams_WithInvalidPagination_Returns400WithFieldErrors()
        {
            var (_, adminToken) = await _authenticatedUser.CreateAdminAsync("admin");

            var response = await GetTeamsAsync(adminToken, "?page=0");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("Page", out _));
        }
    }
}
