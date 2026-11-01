using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Authentication
{
    public sealed class RegisterIntegrationTests(TeamManagerApiFactory factory) : IClassFixture<TeamManagerApiFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private sealed record RegisterResponse(Guid Id);

        [Fact]
        public async Task Register_ValidRequest_ShouldCreateUserAndOutboxMessage()
        {
            var email = $"integration-{Guid.NewGuid():N}@example.com";

            var request = new
            {
                email,
                displayName = "Integration Test User",
                password = "P@ssword1234"
            };

            var response = await _client.PostAsJsonAsync("/api/authentication/register", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);

            using var scope = factory.Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();

            var user = await db.Users.AsNoTracking().SingleAsync(x => x.Email == email);

            Assert.Equal(request.displayName, user.DisplayName);

            Assert.NotEqual(request.password, user.PasswordHash);

            Assert.False(user.IsEmailConfirmed);

            Assert.NotNull(user.EmailConfirmationTokenHash);

            // Assert - Outbox
            var outboxMessage = await db.OutboxMessages.SingleAsync(x => x.Payload.Contains(email));

            Assert.Equal("EmailConfirmationEmail", outboxMessage.Type);
        }
    }
}