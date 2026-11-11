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
    public sealed class AdminUserRoleIntegrationTests(UserFactory factory) : IntegrationTestBase(factory)
    {
        private readonly AdminTestHelpers _helpers = new(factory);

        [Fact]
        public async Task AssignRole_AsAdmin_AssignsRoleToUser()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (targetUserId, _, _) = await _helpers.CreateRegularUserAsync("assignrole-target");
            var customRoleId = await _helpers.CreateRoleAsync($"Role-{Guid.NewGuid():N}", "Custom Role", adminToken);

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.UserRoles(targetUserId), adminToken,
                new { roleId = customRoleId });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            var hasRole = await db.UserRoles.AnyAsync(ur => ur.UserId == targetUserId && ur.RoleId == customRoleId);
            Assert.True(hasRole);
        }

        [Fact]
        public async Task AssignRole_AsRegularUser_Returns403()
        {
            var (_, _, userToken) = await _helpers.CreateRegularUserAsync("assignrole-reg");
            var (targetUserId, _, _) = await _helpers.CreateRegularUserAsync("assignrole-target2");

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.UserRoles(targetUserId), userToken,
                new { roleId = 1 });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AssignRole_WithoutToken_Returns401()
        {
            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.UserRoles(Guid.NewGuid()), null,
                new { roleId = 1 });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AssignRole_WhenUserNotFound_Returns404()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.UserRoles(Guid.NewGuid()), adminToken,
                new { roleId = 1 });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AssignRole_WhenRoleNotFound_Returns404()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (targetUserId, _, _) = await _helpers.CreateRegularUserAsync("assignrole-target-norole");

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.UserRoles(targetUserId), adminToken,
                new { roleId = 999999 });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AssignRole_WhenUserAlreadyHasRole_Returns400()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (targetUserId, _, _) = await _helpers.CreateRegularUserAsync("assignrole-target-dupe");

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var userRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.User);

                // User already has DefaultRoles.User by default upon registration
                var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.UserRoles(targetUserId), adminToken,
                    new { roleId = userRole.Id });

                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task AssignRole_ToDeletedUser_Returns404()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (targetUserId, _, _) = await _helpers.CreateRegularUserAsync("assignrole-target-del");

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var user = await db.Users.SingleAsync(u => u.Id == targetUserId);
                user.SoftDelete();
                await db.SaveChangesAsync(CancellationToken.None);
            }

            var customRoleId = await _helpers.CreateRoleAsync($"Role-{Guid.NewGuid():N}", "Custom Role", adminToken);

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.UserRoles(targetUserId), adminToken,
                new { roleId = customRoleId });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AssignRole_ConcurrentAssignSameRole_ExactlyOneSucceeds()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (targetUserId, _, _) = await _helpers.CreateRegularUserAsync("assignrole-race-target");
            var customRoleId = await _helpers.CreateRoleAsync($"Role-{Guid.NewGuid():N}", "Concurrent Role", adminToken);

            var task1 = _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.UserRoles(targetUserId), adminToken,
                new { roleId = customRoleId });
            var task2 = _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.UserRoles(targetUserId), adminToken,
                new { roleId = customRoleId });

            var responses = await Task.WhenAll(task1, task2);
            var statuses = responses.Select(r => r.StatusCode).OrderBy(c => c).ToArray();

            Assert.Equal(HttpStatusCode.NoContent, statuses[0]);
            Assert.True(statuses[1] == HttpStatusCode.BadRequest || statuses[1] == HttpStatusCode.Conflict);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            var count = await db.UserRoles.CountAsync(ur => ur.UserId == targetUserId && ur.RoleId == customRoleId);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task RevokeRole_AsAdmin_RevokesRoleFromUser()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (targetUserId, _, _) = await _helpers.CreateRegularUserAsync("revokerole-target");
            var customRoleId = await _helpers.CreateRoleAsync($"Role-{Guid.NewGuid():N}", "Custom Role", adminToken);

            // Assign custom role first
            var assign = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.UserRoles(targetUserId), adminToken,
                new { roleId = customRoleId });
            Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

            // Revoke custom role
            var response = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.UserRole(targetUserId, customRoleId), adminToken);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            Assert.False(await db.UserRoles.AnyAsync(ur => ur.UserId == targetUserId && ur.RoleId == customRoleId));
        }

        [Fact]
        public async Task RevokeRole_AsRegularUser_Returns403()
        {
            var (_, _, userToken) = await _helpers.CreateRegularUserAsync("revokerole-reg");

            var response = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.UserRole(Guid.NewGuid(), 1), userToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RevokeRole_WithoutToken_Returns401()
        {
            var response = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.UserRole(Guid.NewGuid(), 1));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RevokeRole_DefaultUserRole_Returns403()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (targetUserId, _, _) = await _helpers.CreateRegularUserAsync("revokerole-target-default");

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            var userRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.User);

            var response = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.UserRole(targetUserId, userRole.Id), adminToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RevokeRole_WhenUserDoesNotHaveRole_Returns400()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (targetUserId, _, _) = await _helpers.CreateRegularUserAsync("revokerole-target-notassigned");
            var customRoleId = await _helpers.CreateRoleAsync($"Role-{Guid.NewGuid():N}", "Unassigned Role", adminToken);

            var response = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.UserRole(targetUserId, customRoleId), adminToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RevokeRole_CannotRevokeLastSystemAdmin_Returns403()
        {
            // Ensure there is only 1 admin
            var (adminUserId, _, adminToken) = await _helpers.CreateAdminUserAsync();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var adminRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.Admin);
                var otherAdmins = await db.UserRoles
                    .Where(ur => ur.RoleId == adminRole.Id && ur.UserId != adminUserId)
                    .ToListAsync();

                if (otherAdmins.Count > 0)
                {
                    db.UserRoles.RemoveRange(otherAdmins);
                    await db.SaveChangesAsync(CancellationToken.None);
                }

                var response = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.UserRole(adminUserId, adminRole.Id), adminToken);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            }
        }

        [Fact]
        public async Task RevokeRole_ConcurrentRevokeOnTwoAdmins_ExactlyOneSucceedsToPreventZeroAdmins()
        {
            // Set up exactly 2 system admins in the system
            var (admin1Id, _, admin1Token) = await _helpers.CreateAdminUserAsync();
            var (admin2Id, _, _) = await _helpers.CreateAdminUserAsync();

            int adminRoleId;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var adminRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.Admin);
                adminRoleId = adminRole.Id;

                // Clean out any other admins besides admin1 and admin2
                var otherAdmins = await db.UserRoles
                    .Where(ur => ur.RoleId == adminRoleId && ur.UserId != admin1Id && ur.UserId != admin2Id)
                    .ToListAsync();

                if (otherAdmins.Count > 0)
                {
                    db.UserRoles.RemoveRange(otherAdmins);
                    await db.SaveChangesAsync(CancellationToken.None);
                }
            }

            // Both admins attempt to revoke their SystemAdmin role simultaneously
            var task1 = _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.UserRole(admin1Id, adminRoleId), admin1Token);
            var task2 = _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.UserRole(admin2Id, adminRoleId), admin1Token);

            var responses = await Task.WhenAll(task1, task2);
            var statuses = responses.Select(r => r.StatusCode).OrderBy(c => c).ToArray();

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.Forbidden], statuses);

            using (var verifyScope = factory.Services.CreateScope())
            {
                var db = verifyScope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
                var remainingAdmins = await db.UserRoles.CountAsync(ur => ur.RoleId == adminRoleId);
                Assert.Equal(1, remainingAdmins);
            }
        }
    }
}
