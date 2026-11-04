using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Projects.Configuration
{
    public sealed class ProjectTestHelpers(TeamManagerApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly TeamTestHelpers _teamHelpers = new(factory);
        public async Task<HttpResponseMessage> CreateProjectRequestAsync(Guid teamId, object body, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, ProjectEndpoints.CreateProject(teamId))
            {
                Content = JsonContent.Create(body)
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        public async Task<(Guid TeamId, Guid ProjectId)> CreateProjectAsync(string name, string ownerToken)
        {
            var teamId = await _teamHelpers.CreateTeamAsync($"Team {Guid.NewGuid():N}", null, ownerToken);
            var response = await CreateProjectRequestAsync(teamId, new { name, description = (string?)null }, ownerToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (teamId, await response.Content.ReadFromJsonAsync<Guid>());
        }
    }
}
