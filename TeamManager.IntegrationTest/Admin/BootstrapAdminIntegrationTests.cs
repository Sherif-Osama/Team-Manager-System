using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Admin.Configuration;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.User;

namespace TeamManager.IntegrationTest.Admin
{
    [Collection("Users")]
    public sealed class BootstrapAdminIntegrationTests(UserFactory factory) : IntegrationTestBase(factory)
    {
        private readonly AdminTestHelpers _helpers = new(factory);
        public const string AdminSecret = "YOUR_NEW_ADMIN_SECRET";

        [Fact]
        public async Task Bootstrap_WithoutToken_Returns401()
        {
            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Bootstrap,
                accessToken: null, body: new { email = "nobody@example.com", secret = AdminSecret });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Bootstrap_WithWrongSecret_Returns401()
        {
            var (_, email, token) = await _helpers.CreateRegularUserAsync("boot-wrongsecret");

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Bootstrap,
                accessToken: token, body: new { email, secret = "invalid-secret-value" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Bootstrap_WhenUserInactive_Returns403()
        {
            var (userId, email, token) = await _helpers.CreateRegularUserAsync("boot-inactive");

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var user = await db.Users.SingleAsync(u => u.Id == userId);
                user.Deactivate();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Bootstrap,
                accessToken: token, body: new { email, secret = AdminSecret });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Bootstrap_WhenAdminAlreadyExists_Returns403()
        {
            // Ensure at least one admin exists
            await _helpers.CreateAdminUserAsync();

            var (_, email, token) = await _helpers.CreateRegularUserAsync("boot-blocked");

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Bootstrap,
                accessToken: token, body: new { email, secret = AdminSecret });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Bootstrap_ConcurrentRequests_ExactlyOneSucceedsAndOtherReturns403()
        {
            // Ensure fresh state where no admin exists for these two candidate users
            var (_, email1, token1) = await _helpers.CreateRegularUserAsync("boot-racer1");
            var (_, email2, token2) = await _helpers.CreateRegularUserAsync("boot-racer2");

            // Clear any existing SystemAdmin user-roles to test race condition accurately
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var adminRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.Admin);
                var existingAdminRoles = await db.UserRoles.Where(ur => ur.RoleId == adminRole.Id).ToListAsync();
                if (existingAdminRoles.Count > 0)
                {
                    db.UserRoles.RemoveRange(existingAdminRoles);
                    await db.SaveChangesAsync(CancellationToken.None);
                }
            }

            var task1 = _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Bootstrap, token1,
                new { email = email1, secret = AdminSecret });
            var task2 = _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Bootstrap, token2,
                new { email = email2, secret = AdminSecret });

            var responses = await Task.WhenAll(task1, task2);
            var statuses = responses.Select(r => r.StatusCode).OrderBy(c => c).ToArray();

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.Forbidden], statuses);

            using (var verifyScope = factory.Services.CreateScope())
            {
                var db = verifyScope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var adminRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.Admin);
                var adminCount = await db.UserRoles.CountAsync(ur => ur.RoleId == adminRole.Id);
                Assert.Equal(1, adminCount);
            }
        }
    }
}
