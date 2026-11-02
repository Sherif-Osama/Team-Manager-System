using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Authentication.Configuration;

namespace TeamManager.IntegrationTest.Authentication
{
    [Collection("Authentication")]
    public sealed class ResetPasswordIntegrationTests(AuthenticationFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

        private readonly string _registerEndpoint = AuthenticationEndpoints.Register;
        private readonly string _loginEndpoint = AuthenticationEndpoints.Login;
        private readonly string _forgotPasswordEndpoint = AuthenticationEndpoints.ForgotPassword;
        private readonly string _resetPasswordEndpoint = AuthenticationEndpoints.ResetPassword;

        private sealed record RegisterResponse(Guid Id);

        private sealed record LoginResponse(string AccessToken, string RefreshToken);

        private async Task<(Guid Id, string Email)> RegisterUserAsync()
        {
            var email = $"reset-{Guid.NewGuid():N}@example.com";

            var response = await _client.PostAsJsonAsync(_registerEndpoint,
                new { email, displayName = "Reset Password User", password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            return (result!.Id, email);
        }

        private async Task ConfirmEmailAsync(Guid userId, string email)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IEmailConfirmationTokenService>();

            var confirmationMessage = await db.OutboxMessages.AsNoTracking()
                .SingleAsync(o => o.Type == "EmailConfirmationEmail" && o.Payload.Contains(email));

            using var payload = JsonDocument.Parse(confirmationMessage.Payload);
            var rawToken = payload.RootElement.GetProperty("Token").GetString();

            var user = await db.Users.SingleAsync(u => u.Id == userId);
            user.ConfirmEmail(tokenService.HashToken(rawToken!));
            await db.SaveChangesAsync(CancellationToken.None);
        }

        private async Task<(Guid UserId, string Email)> RegisterAndConfirmUserAsync()
        {
            var (userId, email) = await RegisterUserAsync();
            await ConfirmEmailAsync(userId, email);
            return (userId, email);
        }

        private async Task<string> RequestPasswordResetTokenAsync(string email)
        {
            var response = await _client.PostAsJsonAsync(_forgotPasswordEndpoint, new { email });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var resetMessage = await db.OutboxMessages.AsNoTracking()
                .OrderByDescending(o => o.Id)
                .FirstAsync(o => o.Type == "PasswordResetEmail" && o.Payload.Contains(email));

            using var payload = JsonDocument.Parse(resetMessage.Payload);
            return payload.RootElement.GetProperty("Token").GetString()!;
        }

        private async Task<HttpResponseMessage> ResetPasswordAsync(string email, string token, string newPassword = "NewPass1234!")
        {
            return await _client.PostAsJsonAsync(_resetPasswordEndpoint,
                new { email, token, newPassword, confirmNewPassword = newPassword });
        }

        [Fact]
        public async Task ResetPassword_WithValidToken_ResetsPasswordRevokesSessionsAndNotifies()
        {
            var (userId, email) = await RegisterAndConfirmUserAsync();

            var loginBefore = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email, password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.OK, loginBefore.StatusCode);

            var token = await RequestPasswordResetTokenAsync(email);

            var response = await ResetPasswordAsync(email, token);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

                var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

                Assert.Null(user.PasswordResetTokenHash);
                Assert.Null(user.PasswordResetTokenExpiresAtUtc);

                var refreshTokens = await db.RefreshTokens.AsNoTracking().Where(r => r.UserId == userId).ToListAsync();

                Assert.NotEmpty(refreshTokens);
                Assert.All(refreshTokens, r => Assert.NotNull(r.RevokedAtUtc));

                Assert.True(await db.OutboxMessages.AnyAsync(o =>
                    o.Type == "PasswordChangedNotificationEmail" && o.Payload.Contains(email)));
            }

            var loginWithOldPassword = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email, password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.Unauthorized, loginWithOldPassword.StatusCode);

            var loginWithNewPassword = await _client.PostAsJsonAsync(_loginEndpoint,
                new { email, password = "NewPass1234!" });

            Assert.Equal(HttpStatusCode.OK, loginWithNewPassword.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithInvalidToken_Returns403()
        {
            var (_, email) = await RegisterAndConfirmUserAsync();

            var response = await ResetPasswordAsync(email, "invalid-token-value");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithUnknownEmail_Returns403()
        {
            var response = await ResetPasswordAsync($"unknown-{Guid.NewGuid():N}@example.com", "some-token");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithExpiredToken_Returns403()
        {
            var (userId, email) = await RegisterAndConfirmUserAsync();
            var token = await RequestPasswordResetTokenAsync(email);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                await db.Users.Where(u => u.Id == userId)
                    .ExecuteUpdateAsync(u => u.SetProperty(u => u.PasswordResetTokenExpiresAtUtc, DateTime.UtcNow.AddMinutes(-5)));
            }

            var response = await ResetPasswordAsync(email, token);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WithUnconfirmedEmail_Returns403()
        {
            var (_, email) = await RegisterUserAsync();

            var response = await ResetPasswordAsync(email, "some-token");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_TokenIsSingleUse()
        {
            var (userId, email) = await RegisterAndConfirmUserAsync();
            var token = await RequestPasswordResetTokenAsync(email);

            var first = await ResetPasswordAsync(email, token);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

            var second = await ResetPasswordAsync(email, token);

            Assert.Equal(HttpStatusCode.Forbidden, second.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

            Assert.Null(user.PasswordResetTokenHash);
        }

        [Fact]
        public async Task ResetPassword_WithMismatchedPasswords_Returns400WithFieldErrors()
        {
            var (_, email) = await RegisterAndConfirmUserAsync();
            var token = await RequestPasswordResetTokenAsync(email);

            var response = await _client.PostAsJsonAsync(_resetPasswordEndpoint,
                new { email, token, newPassword = "NewPass1234!", confirmNewPassword = "Different1234!" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("ConfirmNewPassword", out _));
        }

        [Fact]
        public async Task ResetPassword_WithSoftDeletedUser_Returns403()
        {
            var (userId, email) = await RegisterAndConfirmUserAsync();
            var token = await RequestPasswordResetTokenAsync(email);

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var user = await db.Users.SingleAsync(u => u.Id == userId);
                user.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await ResetPasswordAsync(email, token);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
