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
    public sealed class LoginIntegrationTests(AuthenticationFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly string _registerEndpoint = AuthenticationEndpoints.Register;
        private readonly string _loginEndpoint = AuthenticationEndpoints.Login;
        private sealed record RegisterResponse(Guid Id);

        private sealed record LoginResponse(string AccessToken, string RefreshToken);

        private async Task<(Guid Id, string Email)> RegisterUserAsync(string displayName = "Login Test User")
        {
            var email = $"login-{Guid.NewGuid():N}@example.com";

            var response = await _client.PostAsJsonAsync(_registerEndpoint,
                new { email, displayName, password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            return (result!.Id, email);
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
        public async Task Login_ValidCredentials_ReturnsTokensAndPersistsHashedRefreshToken()
        {
            var (userId, email) = await RegisterUserAsync();

            var response = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email, password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
            Assert.NotEqual(result.AccessToken, result.RefreshToken);

            using (var jwt = DecodeJwtPayload(result.AccessToken))
            {
                var claims = jwt.RootElement;
                Assert.Equal(userId.ToString(), claims.GetProperty(ClaimTypes.NameIdentifier).GetString());
                Assert.Equal(email, claims.GetProperty(ClaimTypes.Email).GetString());
                Assert.Equal("Login Test User", claims.GetProperty(ClaimTypes.Name).GetString());
                var roleClaim = claims.GetProperty(ClaimTypes.Role);
                var roles = roleClaim.ValueKind == JsonValueKind.Array? 
                    roleClaim.EnumerateArray().Select(r => r.GetString()): [roleClaim.GetString()];

                Assert.Contains("User", roles);
            }

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var refreshTokens = await db.RefreshTokens.AsNoTracking().Where(r => r.UserId == userId).ToListAsync();

            var savedToken = Assert.Single(refreshTokens);

            Assert.NotEqual(result.RefreshToken, savedToken.TokenHash);
            Assert.NotNull(savedToken.DeviceInfo);
            Assert.False(savedToken.IsExpired);
            Assert.False(savedToken.IsRevoked);

            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.NotNull(user.LastLoginUtc);
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401AndPersistsFailedAttempt()
        {
            var (userId, email) = await RegisterUserAsync();

            var response = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email, password = "WrongPass123!" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

            Assert.Equal(1, user.FailedLoginAttempts);
            Assert.Empty(await db.RefreshTokens.Where(r => r.UserId == userId).ToListAsync());
        }

        [Fact]
        public async Task Login_AfterFiveFailedAttempts_LocksAccountEvenWithCorrectPassword()
        {
            var (userId, email) = await RegisterUserAsync();

            for (var i = 0; i < 5; i++)
            {
                var failed = await _client.PostAsJsonAsync(_loginEndpoint,
                    new { email, password = "WrongPass123!" });

                Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
            }

            var locked = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email, password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Locked, locked.StatusCode);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

                Assert.Equal(5, user.FailedLoginAttempts);
                Assert.NotNull(user.LockoutEndUtc);
                Assert.True(user.LockoutEndUtc > DateTime.UtcNow);
                Assert.Empty(await db.RefreshTokens.Where(r => r.UserId == userId).ToListAsync());
            }

            var stillLocked = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email, password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Locked, stillLocked.StatusCode);
        }

        [Fact]
        public async Task Login_WithSoftDeletedUser_Returns401()
        {
            var (userId, email) = await RegisterUserAsync();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var user = await db.Users.SingleAsync(u => u.Id == userId);
                user.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email, password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_EmailComparison_IsCaseInsensitive()
        {
            var email = $"Case-{Guid.NewGuid():N}@Example.com";

            var registerResponse = await _client.PostAsJsonAsync(_registerEndpoint,
                new { email, displayName = "Casing User", password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

            var loginResponse = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email = email.ToLowerInvariant(), password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        }

        [Fact]
        public async Task Login_WithInvalidPayload_Returns400WithFieldErrors()
        {
            var response = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email = "", password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("Email", out _));
        }

        [Fact]
        public async Task Login_WithInactiveUser_ReactivatesAccountAndWritesAuditLog()
        {
            var (userId, email) = await RegisterUserAsync();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var user = await db.Users.SingleAsync(u => u.Id == userId);
                user.Deactivate();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email, password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var user2 = await db2.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

            Assert.True(user2.IsActive);

            Assert.True(await db2.AuditLogs.AnyAsync(a =>
                a.EntityType == "User" && a.EntityId == userId.ToString() && a.ActorUserId == null));
        }

        [Fact]
        public async Task Login_ConcurrentLogins_PersistDistinctRefreshTokens()
        {
            var (userId, email) = await RegisterUserAsync();

            var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ =>
                _client.PostAsJsonAsync(_loginEndpoint,
                    new { email, password = "P@ssword1234" })));

            Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

            var tokens = responses.Select(r => r.Content.ReadFromJsonAsync<LoginResponse>()!.Result!.RefreshToken).ToList();

            Assert.Equal(3, tokens.Distinct().Count());

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var persisted = await db.RefreshTokens.AsNoTracking().Where(r => r.UserId == userId).ToListAsync();

            Assert.Equal(3, persisted.Count);
            Assert.Equal(3, persisted.Select(p => p.TokenHash).Distinct().Count());
            Assert.All(persisted, token => Assert.DoesNotContain(token.TokenHash, tokens));
        }
    }
}
