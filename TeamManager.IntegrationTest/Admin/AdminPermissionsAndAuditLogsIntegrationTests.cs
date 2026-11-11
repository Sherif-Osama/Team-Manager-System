using System.Net;
using System.Net.Http.Json;
using TeamManager.Application.Features.AuditLogs.Queries;
using TeamManager.Application.Features.Permissions.Queries.GetPermissionById;
using TeamManager.IntegrationTest.Admin.Configuration;
using TeamManager.IntegrationTest.Infrastructure;
using TeamManager.IntegrationTest.Infrastructure.Collections.User;

namespace TeamManager.IntegrationTest.Admin
{
    [Collection("Users")]
    public sealed class AdminPermissionsAndAuditLogsIntegrationTests(UserFactory factory) : IntegrationTestBase(factory)
    {
        private readonly AdminTestHelpers _helpers = new(factory);

        [Fact]
        public async Task GetPermissions_AsAdmin_ReturnsPermissionsList()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            var result = await _helpers.GetPermissionsResultAsync(adminToken);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Permissions);
            Assert.Contains(result.Permissions, p => p.Code == "system.manage_roles");
        }

        [Fact]
        public async Task GetPermissions_AsRegularUser_Returns403()
        {
            var (_, _, userToken) = await _helpers.CreateRegularUserAsync();

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.Permissions, userToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetPermissions_WithoutToken_Returns401()
        {
            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.Permissions);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetPermissionById_AsAdmin_ReturnsPermissionDetails()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.Permission(1), adminToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var permission = await response.Content.ReadFromJsonAsync<GetPermissionByIdResponse>();
            Assert.NotNull(permission);
            Assert.Equal(1, permission.PermissionId);
            Assert.False(string.IsNullOrWhiteSpace(permission.Code));
        }

        [Fact]
        public async Task GetPermissionById_WhenNotFound_Returns404()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.Permission(999999), adminToken);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetAuditLogs_AsAdmin_ReturnsAuditLogsList()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.AuditLogs, adminToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<GetAuditLogsResponse>();
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
        }

        [Fact]
        public async Task GetAuditLogs_AsRegularUser_Returns403()
        {
            var (_, _, userToken) = await _helpers.CreateRegularUserAsync();

            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.AuditLogs, userToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetAuditLogs_WithoutToken_Returns401()
        {
            var response = await _helpers.RequestAsync(HttpMethod.Get, AdminEndpoints.AuditLogs);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAuditLogs_FiltersByEntityType()
        {
            var (_, _, adminToken) = await _helpers.CreateAdminUserAsync();

            // Perform an admin action that writes an audit log (e.g. create a role)
            await _helpers.CreateRoleAsync($"{Guid.NewGuid():N}", "Description", adminToken);

            var response = await _helpers.RequestAsync(HttpMethod.Get, $"{AdminEndpoints.AuditLogs}?entityType=Role", adminToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<GetAuditLogsResponse>();
            Assert.NotNull(result);
            Assert.All(result.Items, log => Assert.Equal("Role", log.EntityType));
        }

        [Fact]
        public async Task GetAuditLogs_FiltersByActorUserId()
        {
            var (adminUserId, _, adminToken) = await _helpers.CreateAdminUserAsync();

            // Perform an admin action
            await _helpers.CreateRoleAsync($"ActorRole-{Guid.NewGuid():N}", "Description", adminToken);

            var response = await _helpers.RequestAsync(HttpMethod.Get, $"{AdminEndpoints.AuditLogs}?actorUserId={adminUserId}", adminToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<GetAuditLogsResponse>();
            Assert.NotNull(result);
            Assert.All(result.Items, log => Assert.Equal(adminUserId, log.ActorUserId));
        }
    }
}
