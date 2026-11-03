using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Authentication.Configuration;
using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Teams.Configuration
{
    public sealed class TeamTestHelpers(TeamManagerApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

        public sealed record RegisterResponse(Guid Id);

        public sealed record LoginResponse(string AccessToken, string RefreshToken);

        public async Task<(Guid Id, string Email)> RegisterUserAsync(string emailPrefix = "team")
        {
            var email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com";

            var response = await _client.PostAsJsonAsync(AuthenticationEndpoints.Register,
                new { email, displayName = "Team Test User", password = "P@ssword1234" });

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

        public async Task<(Guid Id, string Email, string AccessToken)> CreateConfirmedUserAsync(string emailPrefix = "team")
        {
            var (userId, email) = await RegisterUserAsync(emailPrefix);
            await ConfirmEmailAsync(userId, email);
            var login = await LoginAsync(email);
            return (userId, email, login.AccessToken);
        }

        public async Task PromoteToAdminAsync(Guid userId)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var adminRole = await db.Roles.SingleAsync(r => r.Name == "SystemAdmin");
            var user = await db.Users.SingleAsync(u => u.Id == userId);

            user.AssignRole(adminRole.Id);
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}
