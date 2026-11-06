using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Users.Configuration
{
    public sealed class UserTestHelpers(TeamManagerApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

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

        public Task<HttpResponseMessage> GetMyProfileAsync(string? accessToken = null)
            => RequestAsync(HttpMethod.Get, UserEndpoints.Me, accessToken);

        public async Task<GetMyProfileResponseDto> GetMyProfileResultAsync(string accessToken)
        {
            var response = await GetMyProfileAsync(accessToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<GetMyProfileResponseDto>())!;
        }

        public Task<HttpResponseMessage> UpdateMyProfileRequestAsync(object body, string? accessToken = null)
            => RequestAsync(HttpMethod.Put, UserEndpoints.Me, accessToken, body);

        public async Task UpdateMyProfileAsync(string accessToken, string displayName)
        {
            var response = await UpdateMyProfileRequestAsync(new { displayName }, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public Task<HttpResponseMessage> ChangeEmailRequestAsync(object body, string? accessToken = null)
            => RequestAsync(HttpMethod.Put, UserEndpoints.MyEmail, accessToken, body);

        public Task<HttpResponseMessage> ConfirmEmailRequestAsync(object body, string? accessToken = null)
            => RequestAsync(HttpMethod.Post, UserEndpoints.ConfirmMyEmail, accessToken, body);

        public Task<HttpResponseMessage> ResendEmailConfirmationAsync(string? accessToken = null)
            => RequestAsync(HttpMethod.Post, UserEndpoints.ResendMyEmailConfirmation, accessToken);

        public Task<HttpResponseMessage> ChangePasswordRequestAsync(object body, string? accessToken = null)
            => RequestAsync(HttpMethod.Put, UserEndpoints.MyPassword, accessToken, body);

        public Task<HttpResponseMessage> DeleteMyAccountRequestAsync(object body, string? accessToken = null)
            => RequestAsync(HttpMethod.Delete, UserEndpoints.Me, accessToken, body);

        public async Task DeleteMyAccountAsync(string accessToken, string currentPassword)
        {
            var response = await DeleteMyAccountRequestAsync(new { currentPassword }, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public Task<HttpResponseMessage> DeactivateMyAccountRequestAsync(object body, string? accessToken = null)
            => RequestAsync(HttpMethod.Post, UserEndpoints.DeactivateMe, accessToken, body);

        public async Task DeactivateMyAccountAsync(string accessToken, string currentPassword)
        {
            var response = await DeactivateMyAccountRequestAsync(new { currentPassword }, accessToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public async Task<string> GetLatestEmailConfirmationTokenAsync(string email)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var confirmationMessage = await db.OutboxMessages.AsNoTracking()
                .Where(o => o.Type == "EmailConfirmationEmail" && o.Payload.Contains(email))
                .OrderByDescending(o => o.Id)
                .FirstAsync();

            using var payload = JsonDocument.Parse(confirmationMessage.Payload);

            return payload.RootElement.GetProperty("Token").GetString()!;
        }

        public async Task<int> CountEmailConfirmationMessagesAsync(string email)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            return await db.OutboxMessages.AsNoTracking()
                .CountAsync(o => o.Type == "EmailConfirmationEmail" && o.Payload.Contains(email));
        }

        // System admin counts are global state shared by every test in the collection, so
        // the tests that depend on the exact number of admins start from a clean slate.
        public async Task SoftDeleteAllActiveSystemAdminsAsync()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var adminRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.Admin);

            var admins = await db.Users
                .Where(u => u.DeletedAtUtc == null && u.UserRoles.Any(ur => ur.RoleId == adminRole.Id))
                .ToListAsync();

            foreach (var admin in admins)
            {
                admin.SoftDelete();
            }

            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}
