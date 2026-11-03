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
    public sealed class GetTeamsIntegrationTests(TeamsFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _helpers = new(factory);

        private sealed record CreateTeamResponse(Guid Id);

        private sealed record TeamListItemDto(Guid Id, string Name, string? Description, Guid OwnerUserId,
            string OwnerDisplayName, Guid CreatedBy, string CreatorDisplayName, DateTime CreatedAtUtc);

        private sealed record GetTeamsResponseDto(IReadOnlyCollection<TeamListItemDto> Items, int TotalCount, int Page, int PageSize);

        private async Task<Guid> CreateTeamAsync(string name, string accessToken, string? description = null)
        {
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Teams)
            {
                Content = JsonContent.Create(new { name, description }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<CreateTeamResponse>())!.Id;
        }

        private async Task<HttpResponseMessage> GetTeamsAsync(string accessToken, string query = "")
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TeamEndpoints.Teams}{query}");

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
        public async Task GetTeams_AsAdmin_ReturnsPagedTeams()
        {
            var (_, adminToken) = await CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");

            var firstId = await CreateTeamAsync("First Team", ownerToken);
            var secondId = await CreateTeamAsync("Second Team", ownerToken);

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
            var (_, _, accessToken) = await _helpers.CreateConfirmedUserAsync("regular");

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
            var (_, adminToken) = await CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");

            await CreateTeamAsync("Alpha Team", ownerToken);
            await CreateTeamAsync("Beta Team", ownerToken, "gamma project description");

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
            var (_, adminToken) = await CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");

            var activeId = await CreateTeamAsync("Active Team", ownerToken);
            var inactiveId = await CreateTeamAsync("Inactive Team", ownerToken);

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
            var (_, adminToken) = await CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");

            await CreateTeamAsync("Page One Team", ownerToken);
            await CreateTeamAsync("Page Two Team", ownerToken);
            await CreateTeamAsync("Page Three Team", ownerToken);

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
            var (_, adminToken) = await CreateAdminAsync("admin");
            var (_, _, ownerToken) = await _helpers.CreateConfirmedUserAsync("owner");

            var aliveId = await CreateTeamAsync("Alive Team", ownerToken);
            var deletedId = await CreateTeamAsync("Removed Team", ownerToken);

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
            var (_, adminToken) = await CreateAdminAsync("admin");

            var response = await GetTeamsAsync(adminToken, "?page=0");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("Page", out _));
        }
    }
}
