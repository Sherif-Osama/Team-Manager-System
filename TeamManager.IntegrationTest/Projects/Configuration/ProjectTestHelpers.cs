using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Projects.Configuration
{
    public sealed class ProjectTestHelpers(TeamManagerApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

        public async Task<HttpResponseMessage> CreateProjectAsync(Guid teamId, object body, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, ProjectEndpoints.CreateProject(teamId))
            {
                Content = JsonContent.Create(body)
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }
    }
}
