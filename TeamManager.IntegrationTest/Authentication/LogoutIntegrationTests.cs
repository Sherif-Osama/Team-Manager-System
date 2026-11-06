using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Authentication.Configuration;
using TeamManager.IntegrationTest.Common;

namespace TeamManager.IntegrationTest.Authentication
{
    [Collection("Authentication")]
    public sealed class LogoutIntegrationTests(AuthenticationFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly AuthenticatedUser _authenticatedUser = new AuthenticatedUser(factory);

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
            var (userId, email) = await _authenticatedUser.RegisterUserAsync();
            var login = await _authenticatedUser.LoginAsync(email);

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
            var (userId, email) = await _authenticatedUser.RegisterUserAsync();
            var login = await _authenticatedUser.LoginAsync(email);

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
            var (userId, email) = await _authenticatedUser.RegisterUserAsync();
            var login = await _authenticatedUser.LoginAsync(email);

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
            var (userId, email) = await _authenticatedUser.RegisterUserAsync();
            var login = await _authenticatedUser.LoginAsync(email);

            var response = await _client.SendAsync(CreateLogoutRequest(login.RefreshToken));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(1, await db.RefreshTokens.CountAsync(r => r.UserId == userId && r.RevokedAtUtc == null));
        }


        [Fact]
        public async Task Logout_DoesNotRevokeOtherSessionTokens()
        {
            var (userId, email) = await _authenticatedUser.RegisterUserAsync();
            var firstSession = await _authenticatedUser.LoginAsync(email);
            var secondSession = await _authenticatedUser.LoginAsync(email);

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
