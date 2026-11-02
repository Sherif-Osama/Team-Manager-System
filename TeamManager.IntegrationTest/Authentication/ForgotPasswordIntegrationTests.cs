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
    public sealed class ForgotPasswordIntegrationTests(AuthenticationFixture factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

        private readonly string _registerEndpoint = AuthenticationEndpoints.Register;
        private readonly string _forgotPasswordEndpoint = AuthenticationEndpoints.ForgotPassword;

        private sealed record RegisterResponse(Guid Id);

        private async Task<(Guid Id, string Email)> RegisterUserAsync()
        {
            var email = $"forgot-{Guid.NewGuid():N}@example.com";

            var response = await _client.PostAsJsonAsync(_registerEndpoint,
                new { email, displayName = "Forgot Password User", password = "P@ssword1234" });

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

        [Fact]
        public async Task ForgotPassword_WithKnownConfirmedEmail_QueuesResetEmailAndSetsToken()
        {
            var (userId, email) = await RegisterAndConfirmUserAsync();

            var response = await _client.PostAsJsonAsync(_forgotPasswordEndpoint,
                new { email = email.ToUpperInvariant() });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.TryGetProperty("message", out _));

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

            Assert.NotNull(user.PasswordResetTokenHash);
            Assert.True(user.PasswordResetTokenExpiresAtUtc > DateTime.UtcNow);

            var resetMessage = await db.OutboxMessages.AsNoTracking()
                .SingleAsync(o => o.Type == "PasswordResetEmail" && o.Payload.Contains(email));

            using var payload = JsonDocument.Parse(resetMessage.Payload);
            Assert.Equal(email, payload.RootElement.GetProperty("To").GetString());
            Assert.False(string.IsNullOrWhiteSpace(payload.RootElement.GetProperty("Token").GetString()));
        }

        [Fact]
        public async Task ForgotPassword_WithUnknownEmail_Returns200WithoutQueuing()
        {
            var unknownEmail = $"unknown-{Guid.NewGuid():N}@example.com";

            var response = await _client.PostAsJsonAsync(_forgotPasswordEndpoint,
                new { email = unknownEmail });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Empty(await db.OutboxMessages.Where(o => o.Payload.Contains(unknownEmail)).ToListAsync());
        }

        [Fact]
        public async Task ForgotPassword_WithUnconfirmedEmail_Returns200WithoutQueuing()
        {
            var (userId, email) = await RegisterUserAsync();

            var response = await _client.PostAsJsonAsync(_forgotPasswordEndpoint,
                new { email });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

            Assert.Null(user.PasswordResetTokenHash);
            Assert.Empty(await db.OutboxMessages.Where(o => o.Type == "PasswordResetEmail" && o.Payload.Contains(email)).ToListAsync());
        }

        [Fact]
        public async Task ForgotPassword_WithSoftDeletedUser_Returns200WithoutQueuing()
        {
            var (userId, email) = await RegisterAndConfirmUserAsync();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var user = await db.Users.SingleAsync(u => u.Id == userId);
                user.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _client.PostAsJsonAsync(_forgotPasswordEndpoint,
                new { email });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            Assert.Empty(await db2.OutboxMessages.Where(o => o.Type == "PasswordResetEmail" && o.Payload.Contains(email)).ToListAsync());
        }

        [Fact]
        public async Task ForgotPassword_WithInvalidPayload_Returns400WithFieldErrors()
        {
            var response = await _client.PostAsJsonAsync(_forgotPasswordEndpoint,
                new { email = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.TryGetProperty("errors", out var errors));
            Assert.True(errors.TryGetProperty("Email", out _));
        }

        [Fact]
        public async Task ForgotPassword_IssuedTwice_ReplacesPreviousTokenAndQueuesAgain()
        {
            var (userId, email) = await RegisterAndConfirmUserAsync();

            var first = await _client.PostAsJsonAsync(_forgotPasswordEndpoint, new { email });

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            string firstHash = string.Empty;

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                firstHash = (await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId)).PasswordResetTokenHash!;
            }

            var second = await _client.PostAsJsonAsync(_forgotPasswordEndpoint, new { email });

            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            using var scope2 = factory.Services.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var user = await db2.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

            Assert.NotNull(user.PasswordResetTokenHash);
            Assert.NotEqual(firstHash, user.PasswordResetTokenHash);
            Assert.Equal(2, await db2.OutboxMessages.CountAsync(o => o.Type == "PasswordResetEmail" && o.Payload.Contains(email)));
        }
    }
}
