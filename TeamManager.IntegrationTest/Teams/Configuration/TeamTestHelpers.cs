using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Teams.Configuration
{
    public sealed class TeamTestHelpers(TeamManagerApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private sealed record CreateTeamResponse(Guid Id);
        private sealed record AddedMemberResponse(long Id);

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

        public async Task<HttpResponseMessage> TransferOwnershipAsync(Guid teamId, Guid newOwnerUserId, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, TeamEndpoints.TeamOwner(teamId))
            {
                Content = JsonContent.Create(new { newOwnerUserId })
            };

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

        public async Task<HttpResponseMessage> AddMemberAsync(Guid teamId, Guid userId, TeamRole role, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, TeamEndpoints.Members(teamId))
            {
                Content = JsonContent.Create(new { userId, teamRole = role })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        public async Task<long> AddMemberReturningIdAsync(Guid teamId, Guid userId, TeamRole role, string accessToken)
        {
            var response = await AddMemberAsync(teamId, userId, role, accessToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<AddedMemberResponse>())!.Id;
        }

        public async Task<HttpResponseMessage> RemoveMemberAsync(Guid teamId, long memberId, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, TeamEndpoints.Member(teamId, memberId));

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> ChangeMemberRoleAsync(Guid teamId, long memberId, TeamRole role, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, TeamEndpoints.MemberRole(teamId, memberId))
            {
                Content = JsonContent.Create(new { role })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> GetMembersAsync(Guid teamId, string? accessToken = null, string query = "")
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{TeamEndpoints.Members(teamId)}{query}");

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> GetMemberAsync(Guid teamId, long memberId, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, TeamEndpoints.Member(teamId, memberId));

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await _client.SendAsync(request);
        }
    }
}
