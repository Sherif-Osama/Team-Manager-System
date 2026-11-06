using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Authentication.Configuration;
using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Common
{
    public sealed class AuthenticatedUser(TeamManagerApiFactory factory)
    {
        public const string DefaultDisplayName = "User";

        private readonly HttpClient _client = factory.CreateClient();
        public sealed record RegisterResponse(Guid Id);
        public sealed record LoginResponse(string AccessToken, string RefreshToken);

        public async Task<(Guid Id, string Email)> RegisterUserAsync(string emailPrefix = "User", string password = "P@ssword1234",
            string displayName = DefaultDisplayName)
        {
            var email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com";

            var response = await _client.PostAsJsonAsync(AuthenticationEndpoints.Register,
                new { email, displayName, password });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            return (result!.Id, email);
        }

        public async Task<(Guid Id, string Email)> RegisterUserWithEmailAsync(string email, string password = "P@ssword1234")
        {
            var response = await _client.PostAsJsonAsync(AuthenticationEndpoints.Register,
                new { email, displayName = DefaultDisplayName, password });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            return (result!.Id, email);
        }

        public async Task ConfirmEmailAsync(Guid userId, string email)
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

        public async Task<LoginResponse> LoginAsync(string email)
        {
            var response = await _client.PostAsJsonAsync(AuthenticationEndpoints.Login,
                new { email, password = "P@ssword1234" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        }

        public async Task<(Guid Id, string Email, string AccessToken)> CreateConfirmedUserAsync(string emailPrefix = "User",
            string displayName = DefaultDisplayName)
        {
            var (userId, email) = await RegisterUserAsync(emailPrefix, displayName: displayName);
            await ConfirmEmailAsync(userId, email);
            var login = await LoginAsync(email);
            return (userId, email, login.AccessToken);
        }

        public async Task<(Guid UserId, string Email)> RegisterAndConfirmUserAsync()
        {
            var (userId, email) = await RegisterUserAsync();
            await ConfirmEmailAsync(userId, email);
            return (userId, email);
        }

        public JsonDocument DecodeJwtPayload(string token)
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

        public async Task PromoteToAdminAsync(Guid userId)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var adminRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.Admin);
            var user = await db.Users.SingleAsync(u => u.Id == userId);

            user.AssignRole(adminRole.Id);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        public async Task<(Guid Id, string AccessToken)> CreateAdminAsync(string prefix)
        {
            var (userId, _, accessToken) = await CreateConfirmedUserAsync(prefix);
            await PromoteToAdminAsync(userId);
            return (userId, accessToken);
        }
    }
}