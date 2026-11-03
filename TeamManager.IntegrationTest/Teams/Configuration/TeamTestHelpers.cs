using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Teams.Configuration
{
    public sealed class TeamTestHelpers(TeamManagerApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private sealed record CreateTeamResponse(Guid Id);

        public async Task<Guid> CreateTeamAsync(string name, string? description, string accessToken)
        {
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Teams)
            {
                Content = JsonContent.Create(new { name, description = description }),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<CreateTeamResponse>())!.Id;
        }

        public HttpRequestMessage CreateTeamRequest(string name, string? description, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Teams)
            {
                Content = JsonContent.Create(new { name, description })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return request;
        }

        public async Task<HttpResponseMessage> GetTeamAsync(Guid teamId, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, TeamEndpoints.Team(teamId));

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> GetByNameAsync(string name, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TeamEndpoints.TeamsByName}?name={Uri.EscapeDataString(name)}");

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }
    }
}
