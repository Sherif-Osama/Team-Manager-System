using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TeamManager.Domain.Enums;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Teams.Configuration;

namespace TeamManager.IntegrationTest.Notifications.Configuration
{
    public sealed class NotificationTestHelpers(TeamManagerApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

        private sealed record InviteMemberResponse(Guid Id);

        public async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string url, string? accessToken = null,
            object? body = null)
        {
            var request = new HttpRequestMessage(method, url);

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            if (body is not null)
                request.Content = JsonContent.Create(body);

            return await _client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> GetNotificationsAsync(string? accessToken = null, string? query = null)
            => await RequestAsync(HttpMethod.Get,
                query is null ? NotificationEndpoints.Notifications : $"{NotificationEndpoints.Notifications}?{query}", accessToken);

        public async Task<GetMyNotificationsResponseDto> GetNotificationsResultAsync(string accessToken, string? query = null)
        {
            var response = await GetNotificationsAsync(accessToken, query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetMyNotificationsResponseDto>())!;
        }

        public async Task<HttpResponseMessage> GetUnreadCountAsync(string? accessToken = null)
            => await RequestAsync(HttpMethod.Get, NotificationEndpoints.UnreadCount, accessToken);

        public async Task<int> GetUnreadCountResultAsync(string accessToken)
        {
            var response = await GetUnreadCountAsync(accessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return await response.Content.ReadFromJsonAsync<int>();
        }

        public async Task<HttpResponseMessage> MarkAsReadRequestAsync(long notificationId, string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, NotificationEndpoints.Read(notificationId), accessToken);

        public async Task MarkAsReadAsync(long notificationId, string accessToken)
        {
            var response = await MarkAsReadRequestAsync(notificationId, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> MarkAllAsReadRequestAsync(string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, NotificationEndpoints.ReadAll, accessToken);

        public async Task MarkAllAsReadAsync(string accessToken)
        {
            var response = await MarkAllAsReadRequestAsync(accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<HttpResponseMessage> GetPreferencesAsync(string? accessToken = null)
            => await RequestAsync(HttpMethod.Get, NotificationEndpoints.Preferences, accessToken);

        public async Task<GetMyNotificationPreferencesResponseDto> GetPreferencesResultAsync(string accessToken)
        {
            var response = await GetPreferencesAsync(accessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetMyNotificationPreferencesResponseDto>())!;
        }

        public async Task<HttpResponseMessage> UpdatePreferencesRequestAsync(object body, string? accessToken = null)
            => await RequestAsync(HttpMethod.Put, NotificationEndpoints.Preferences, accessToken, body);

        public async Task UpdatePreferencesAsync(string accessToken, params (NotificationType Type, bool IsEnabled)[] preferences)
        {
            var response = await UpdatePreferencesRequestAsync(
                new { preferences = preferences.Select(x => new { notificationType = x.Type, isEnabled = x.IsEnabled }).ToArray() },
                accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        // The invitation notification only exists when the invited email already belongs to an account,
        // because that is what fills TeamInvitation.InvitedUserId.
        public async Task<Guid> InviteRegisteredUserAsync(Guid teamId, string email, string ownerToken)
        {
            var response = await RequestAsync(HttpMethod.Post, TeamEndpoints.Invitations(teamId), ownerToken,
                new { email, teamRole = TeamRole.Member });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<InviteMemberResponse>())!.Id;
        }
    }
}
