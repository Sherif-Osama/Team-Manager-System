using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Authentication
{
    public sealed class LogoutIntegrationTests(TeamManagerApiFactory factory) : IClassFixture<TeamManagerApiFactory>
    {
        private readonly HttpClient _client = CreateClient(factory);

        private readonly string _registerEndpoint = AuthenticationEndpoints.Register;
        private readonly string _loginEndpoint = AuthenticationEndpoints.Login;
        private sealed record RegisterResponse(Guid Id);

        private sealed record LoginResponse(string AccessToken, string RefreshToken);

        private static HttpClient CreateClient(TeamManagerApiFactory factory)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "integration-tests/1.0");
            return client;
        }

        private async Task<(Guid Id, string Email)> RegisterUserAsync()
        {
            var email = $"logout-{Guid.NewGuid():N}@example.com";

            var response = await _client.PostAsJsonAsync(_registerEndpoint,
                new { email, displayName = "Logout Test User", password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            return (result!.Id, email);
        }

        private async Task<LoginResponse> LoginAsync(string email)
        {
            var response = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email, password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        }

        private static HttpRequestMessage CreateLogoutRequest(string refreshToken, string? accessToken = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, AuthenticationEndpoints.Logout)
            {
                Content = JsonContent.Create(new { refreshToken })
            };

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return request;
        }

        [Fact]
        public async Task Logout_WithValidToken_RevokesRefreshToken()
        {
            var (userId, email) = await RegisterUserAsync();
            var login = await LoginAsync(email);

            var response = await _client.SendAsync(CreateLogoutRequest(login.RefreshToken, login.AccessToken));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var token = await db.RefreshTokens.AsNoTracking().SingleAsync(r => r.UserId == userId);

            Assert.NotNull(token.RevokedAtUtc);
            Assert.Null(token.ReplacedByTokenId);

            var refreshAttempt = await _client.PostAsJsonAsync(AuthenticationEndpoints.Refresh,
                new { refreshToken = login.RefreshToken });

            Assert.Equal(HttpStatusCode.Unauthorized, refreshAttempt.StatusCode);
        }

        [Fact]
        public async Task Logout_WithUnknownToken_Returns204WithoutChanges()
        {
            var (userId, email) = await RegisterUserAsync();
            var login = await LoginAsync(email);

            var response = await _client.SendAsync(CreateLogoutRequest("not-a-real-token-value", login.AccessToken));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var token = await db.RefreshTokens.AsNoTracking().SingleAsync(r => r.UserId == userId);

            Assert.Null(token.RevokedAtUtc);
        }

        [Fact]
        public async Task Logout_WithAlreadyRevokedToken_IsIdempotent()
        {
            var (userId, email) = await RegisterUserAsync();
            var login = await LoginAsync(email);

            var first = await _client.SendAsync(CreateLogoutRequest(login.RefreshToken, login.AccessToken));
            var second = await _client.SendAsync(CreateLogoutRequest(login.RefreshToken, login.AccessToken));

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(1, await db.RefreshTokens.CountAsync(r => r.UserId == userId));
            Assert.Equal(1, await db.RefreshTokens.CountAsync(r => r.UserId == userId && r.RevokedAtUtc != null));
        }

        [Fact]
        public async Task Logout_WithoutAccessToken_Returns401()
        {
            var (userId, email) = await RegisterUserAsync();
            var login = await LoginAsync(email);

            var response = await _client.SendAsync(CreateLogoutRequest(login.RefreshToken));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(1, await db.RefreshTokens.CountAsync(r => r.UserId == userId && r.RevokedAtUtc == null));
        }

        [Fact]
        public async Task Logout_WithInvalidPayload_Returns400WithFieldErrors()
        {
            var (_, email) = await RegisterUserAsync();
            var login = await LoginAsync(email);

            var response = await _client.SendAsync(CreateLogoutRequest("", login.AccessToken));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("RefreshToken", out _));
        }

        [Fact]
        public async Task Logout_DoesNotRevokeOtherSessionTokens()
        {
            var (userId, email) = await RegisterUserAsync();
            var firstSession = await LoginAsync(email);
            var secondSession = await LoginAsync(email);

            var logout = await _client.SendAsync(CreateLogoutRequest(firstSession.RefreshToken, firstSession.AccessToken));

            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var tokens = await db.RefreshTokens.AsNoTracking().Where(r => r.UserId == userId).ToListAsync();

                Assert.Equal(2, tokens.Count);
                Assert.Equal(1, tokens.Count(t => t.RevokedAtUtc != null));
                Assert.Equal(1, tokens.Count(t => t.RevokedAtUtc == null));
            }

            var refreshOtherSession = await _client.PostAsJsonAsync(AuthenticationEndpoints.Refresh,
                new { refreshToken = secondSession.RefreshToken });

            Assert.Equal(HttpStatusCode.OK, refreshOtherSession.StatusCode);
        }
    }
}
