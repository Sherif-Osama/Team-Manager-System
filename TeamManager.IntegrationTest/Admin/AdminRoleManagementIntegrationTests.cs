using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Infrastructure.Persistence;
using TeamManager.IntegrationTest.Admin.Configuration;
using TeamManager.IntegrationTest.Infrastructure;

namespace TeamManager.IntegrationTest.Admin
{
    [Collection("Common")]
    public sealed class AdminRoleManagementIntegrationTests(TeamManagerApiFactory factory) : IntegrationTestBase(factory)
    {
        private readonly AdminTestHelpers _helpers = new(factory);

        [Fact]
        public async Task CreateRole_AsAdmin_CreatesRoleWith201AndSetsLocation()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var roleName = $"{Guid.NewGuid():N}";

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Roles, adminToken,
                new { Name = roleName, Description = "A test role" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);

            var location = response.Headers.Location.ToString();
            var roleId = int.Parse(location.Substring(location.LastIndexOf('/') + 1));

            var role = await _helpers.GetRoleByIdResultAsync(roleId, adminToken);
            Assert.Equal(roleName, role.Name);
            Assert.Equal("A test role", role.Description);
        }

        [Fact]
        public async Task CreateRole_AsRegularUser_Returns403()
        {
            var (_, _, userToken) = await _helpers.CreateRegularUserAsync();

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Roles, userToken,
                new { name = "Unauthorized Role" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateRole_WithoutToken_Returns401()
        {
            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Roles, null,
                new { name = "No Token Role" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateRole_WithDuplicateName_Returns409()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var roleName = $"Role-{Guid.NewGuid():N}";

            var first = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Roles, adminToken,
                new { name = roleName });
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Roles, adminToken,
                new { name = roleName });
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task CreateRole_ConcurrentWithSameName_ExactlyOneSucceeds()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var roleName = $"RoleRace-{Guid.NewGuid():N}";

            var task1 = _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Roles, adminToken, new { name = roleName });
            var task2 = _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.Roles, adminToken, new { name = roleName });

            var responses = await Task.WhenAll(task1, task2);
            var statuses = responses.Select(r => r.StatusCode).OrderBy(c => c).ToArray();

            Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], statuses);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            var count = await db.Roles.CountAsync(r => r.Name == roleName);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task RenameRole_AsAdmin_RenamesRole()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var originalName = $"Orig-{Guid.NewGuid():N}";
            var newName = $"Renamed-{Guid.NewGuid():N}";

            var roleId = await _helpers.CreateRoleAsync(originalName, null, adminToken);

            var response = await _helpers.RequestAsync(HttpMethod.Put, AdminEndpoints.RoleName(roleId), adminToken,
                new { newName });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var role = await _helpers.GetRoleByIdResultAsync(roleId, adminToken);
            Assert.Equal(newName, role.Name);
        }

        [Fact]
        public async Task RenameRole_BuiltInRoles_Returns403()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            var adminRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.Admin);
            var userRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.User);

            var adminRename = await _helpers.RequestAsync(HttpMethod.Put, AdminEndpoints.RoleName(adminRole.Id), adminToken,
                new { newName = "NewAdminName" });
            Assert.Equal(HttpStatusCode.Forbidden, adminRename.StatusCode);

            var userRename = await _helpers.RequestAsync(HttpMethod.Put, AdminEndpoints.RoleName(userRole.Id), adminToken,
                new { newName = "NewUserName" });
            Assert.Equal(HttpStatusCode.Forbidden, userRename.StatusCode);
        }

        [Fact]
        public async Task RenameRole_WithDuplicateName_Returns409()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var name1 = $"Role1-{Guid.NewGuid():N}";
            var name2 = $"Role2-{Guid.NewGuid():N}";

            await _helpers.CreateRoleAsync(name1, null, adminToken);
            var role2Id = await _helpers.CreateRoleAsync(name2, null, adminToken);

            var response = await _helpers.RequestAsync(HttpMethod.Put, AdminEndpoints.RoleName(role2Id), adminToken,
                new { newName = name1 });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task RenameRole_WhenRoleNotFound_Returns404()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            var response = await _helpers.RequestAsync(HttpMethod.Put, AdminEndpoints.RoleName(999999), adminToken,
                new { newName = "NewName" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteRole_AsAdmin_DeletesCustomRole()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var roleName = $"DeleteMe-{Guid.NewGuid():N}";
            var roleId = await _helpers.CreateRoleAsync(roleName, null, adminToken);

            var response = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.Role(roleId), adminToken);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            Assert.False(await db.Roles.AnyAsync(r => r.Id == roleId));
        }

        [Fact]
        public async Task DeleteRole_BuiltInRoles_Returns403()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            var adminRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.Admin);
            var userRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.User);

            var adminDelete = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.Role(adminRole.Id), adminToken);
            Assert.Equal(HttpStatusCode.Forbidden, adminDelete.StatusCode);

            var userDelete = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.Role(userRole.Id), adminToken);
            Assert.Equal(HttpStatusCode.Forbidden, userDelete.StatusCode);
        }

        [Fact]
        public async Task DeleteRole_WhenAssignedToUsers_Returns409()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var (targetUserId, _, _) = await _helpers.CreateRegularUserAsync();
            var roleId = await _helpers.CreateRoleAsync($"RoleInUse-{Guid.NewGuid():N}", null, adminToken);

            var assign = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.UserRoles(targetUserId), adminToken,
                new { roleId });
            Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

            var delete = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.Role(roleId), adminToken);
            Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
        }

        [Fact]
        public async Task AddPermissionToRole_AsAdmin_AddsPermissionToCustomRole()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var roleId = await _helpers.CreateRoleAsync($"{Guid.NewGuid():N}", null, adminToken);

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.RolePermissions(roleId), adminToken,
                new { permissionId = 1 });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var role = await _helpers.GetRoleByIdResultAsync(roleId, adminToken);
            Assert.Contains(role.Permissions, p => p.PermissionId == 1);
        }

        [Fact]
        public async Task AddPermissionToRole_BuiltInRoles_Returns403()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            var userRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.User);

            var response = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.RolePermissions(userRole.Id), adminToken,
                new { permissionId = 1 });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddPermissionToRole_WhenAlreadyHasPermission_Returns400()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var roleId = await _helpers.CreateRoleAsync($"DupePermRole-{Guid.NewGuid():N}", null, adminToken);

            var first = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.RolePermissions(roleId), adminToken,
                new { permissionId = 1 });
            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

            var second = await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.RolePermissions(roleId), adminToken,
                new { permissionId = 1 });
            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task AddPermissionToRole_ConcurrentAddSamePermission_ExactlyOneSucceeds()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var roleId = await _helpers.CreateRoleAsync($"RacePermRole-{Guid.NewGuid():N}", null, adminToken);

            var task1 = _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.RolePermissions(roleId), adminToken,
                new { permissionId = 2 });
            var task2 = _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.RolePermissions(roleId), adminToken,
                new { permissionId = 2 });

            var responses = await Task.WhenAll(task1, task2);
            var statuses = responses.Select(r => r.StatusCode).OrderBy(c => c).ToArray();

            Assert.Equal(HttpStatusCode.NoContent, statuses[0]);
            Assert.True(statuses[1] == HttpStatusCode.BadRequest || statuses[1] == HttpStatusCode.Conflict);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            var count = await db.RolePermissions.CountAsync(rp => rp.RoleId == roleId && rp.PermissionId == 2);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task RemovePermissionFromRole_AsAdmin_RemovesPermission()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var roleId = await _helpers.CreateRoleAsync($"RemovePermRole-{Guid.NewGuid():N}", null, adminToken);

            await _helpers.RequestAsync(HttpMethod.Post, AdminEndpoints.RolePermissions(roleId), adminToken,
                new { permissionId = 1 });

            var response = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.RolePermission(roleId, 1), adminToken);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var role = await _helpers.GetRoleByIdResultAsync(roleId, adminToken);
            Assert.DoesNotContain(role.Permissions, p => p.PermissionId == 1);
        }

        [Fact]
        public async Task RemovePermissionFromRole_BuiltInRoles_Returns403()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeamManagerDbContext>();
            var adminRole = await db.Roles.SingleAsync(r => r.Name == DefaultRoles.Admin);

            var response = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.RolePermission(adminRole.Id, 1), adminToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RemovePermissionFromRole_WhenRoleDoesNotHavePermission_Returns400()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();
            var roleId = await _helpers.CreateRoleAsync($"NoPermRole-{Guid.NewGuid():N}", null, adminToken);

            var response = await _helpers.RequestAsync(HttpMethod.Delete, AdminEndpoints.RolePermission(roleId, 1), adminToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetRoles_AsAdmin_ReturnsAllRoles()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            var result = await _helpers.GetRolesResultAsync(adminToken);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Roles);
            Assert.Contains(result.Roles, r => r.Name == DefaultRoles.Admin);
            Assert.Contains(result.Roles, r => r.Name == DefaultRoles.User);
        }

        [Fact]
        public async Task GetRoleById_WhenNotFound_Returns404()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.Role(999999), adminToken);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
