using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Authentication.Configuration;

namespace TeamManager.IntegrationTest.Authentication
{
    [Collection("Authentication")]
    public sealed class RefreshIntegrationTests(AuthenticationFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly string _refreshTokenEndpoint = AuthenticationEndpoints.Refresh;
        private readonly string _registerEndpoint = AuthenticationEndpoints.Register;
        private readonly string _loginEndpoint = AuthenticationEndpoints.Login;
        private sealed record RegisterResponse(Guid Id);

        private sealed record LoginResponse(string AccessToken, string RefreshToken);

        private async Task<(Guid Id, string Email)> RegisterUserAsync()
        {
            var email = $"refresh-{Guid.NewGuid():N}@example.com";

            var response = await _client.PostAsJsonAsync(_registerEndpoint,
                new { email, displayName = "Refresh Test User", password = "P@ssword1234" });

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

        private static JsonDocument DecodeJwtPayload(string token)
        {
            var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');

            payload = (payload.Length % 4) switch
            {
                2 => payload + "==",
                3 => payload + "=",
                _ => payload
            };

            return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        }

        [Fact]
        public async Task Refresh_ValidToken_RotatesRefreshTokenAndReturnsNewAccessToken()
        {
            var (userId, email) = await RegisterUserAsync();
            var login = await LoginAsync(email);

            var response = await _client.PostAsJsonAsync(_refreshTokenEndpoint,
                new { refreshToken = login.RefreshToken });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
            Assert.NotEqual(login.RefreshToken, result.RefreshToken);

            using (var jwt = DecodeJwtPayload(result.AccessToken))
            {
                Assert.Equal(userId.ToString(), jwt.RootElement.GetProperty(ClaimTypes.NameIdentifier).GetString());
            }

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var tokens = await db.RefreshTokens.AsNoTracking().Where(r => r.UserId == userId).ToListAsync();

            Assert.Equal(2, tokens.Count);

            var oldToken = tokens.Single(t => t.RevokedAtUtc != null);
            var newToken = tokens.Single(t => t.RevokedAtUtc == null);

            Assert.NotNull(oldToken.RevokedAtUtc);
            Assert.Equal(newToken.Id, oldToken.ReplacedByTokenId);

            Assert.False(newToken.IsRevoked);
            Assert.False(newToken.IsExpired);
            Assert.NotEqual(result.RefreshToken, newToken.TokenHash);
        }

        [Fact]
        public async Task Refresh_WithUnknownToken_Returns401()
        {
            var response = await _client.PostAsJsonAsync(_refreshTokenEndpoint,
                new { refreshToken = "not-a-real-token-value" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_WithRevokedToken_Returns401()
        {
            var (userId, email) = await RegisterUserAsync();
            var login = await LoginAsync(email);

            var firstRefresh = await _client.PostAsJsonAsync(_refreshTokenEndpoint,
                new { refreshToken = login.RefreshToken });

            Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

            var replay = await _client.PostAsJsonAsync(_refreshTokenEndpoint,
                new { refreshToken = login.RefreshToken });

            Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Equal(2, await db.RefreshTokens.CountAsync(r => r.UserId == userId));
            Assert.Equal(1, await db.RefreshTokens.CountAsync(r => r.UserId == userId && r.RevokedAtUtc == null));
        }

        [Fact]
        public async Task Refresh_WithExpiredToken_Returns401()
        {
            var (userId, email) = await RegisterUserAsync();
            var login = await LoginAsync(email);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

                await db.RefreshTokens.Where(r => r.UserId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-5)));
            }

            var response = await _client.PostAsJsonAsync(_refreshTokenEndpoint,
                new { refreshToken = login.RefreshToken });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_WithInactiveUser_Returns401()
        {
            var (userId, email) = await RegisterUserAsync();
            var login = await LoginAsync(email);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var user = await db.Users.SingleAsync(u => u.Id == userId);
                user.Deactivate();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _client.PostAsJsonAsync(_refreshTokenEndpoint,
                new { refreshToken = login.RefreshToken });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_WithSoftDeletedUser_Returns401()
        {
            var (userId, email) = await RegisterUserAsync();
            var login = await LoginAsync(email);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var user = await db.Users.SingleAsync(u => u.Id == userId);
                user.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _client.PostAsJsonAsync(_refreshTokenEndpoint,
                new { refreshToken = login.RefreshToken });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_WithInvalidPayload_Returns400WithFieldErrors()
        {
            var response = await _client.PostAsJsonAsync(_refreshTokenEndpoint,
                new { refreshToken = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("RefreshToken", out _));
        }

        [Fact]
        public async Task Refresh_ConcurrentRequestsWithSameToken_ExactlyOneSucceeds()
        {
            var (userId, email) = await RegisterUserAsync();
            var login = await LoginAsync(email);

            var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
                _client.PostAsJsonAsync(_refreshTokenEndpoint,
                    new { refreshToken = login.RefreshToken })));

            var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);

            Assert.Equal(1, successCount);
            Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.OK),
                r => Assert.True(r.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Conflict));

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var tokens = await db.RefreshTokens.AsNoTracking().Where(r => r.UserId == userId).ToListAsync();

            Assert.Equal(2, tokens.Count);
            Assert.Single(tokens, t => !t.IsRevoked);
            Assert.Single(tokens, t => t.IsRevoked);
        }
    }
}
