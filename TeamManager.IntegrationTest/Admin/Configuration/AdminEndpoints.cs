namespace TeamManager.IntegrationTest.Admin.Configuration
{
    public static class AdminEndpoints
    {
        public const string Bootstrap = "/api/admin/bootstrap";
        public const string Users = "/api/admin/users";
        public const string AuditLogs = "/api/admin/audit-logs";
        public const string Permissions = "/api/admin/permissions";
        public const string Roles = "/api/admin/roles";

        public static string UserRoles(Guid userId) => $"/api/admin/users/{userId}/roles";
        public static string UserRole(Guid userId, int roleId) => $"/api/admin/users/{userId}/roles/{roleId}";
        public static string UserById(Guid userId) => $"/api/admin/users/{userId}";
        public static string UserByEmail(string email) => $"/api/admin/users/by-email/{Uri.EscapeDataString(email)}";

        public static string Role(int roleId) => $"/api/admin/roles/{roleId}";
        public static string RoleName(int roleId) => $"/api/admin/roles/{roleId}/name";
        public static string RolePermissions(int roleId) => $"/api/admin/roles/{roleId}/permissions";
        public static string RolePermission(int roleId, int permissionId) => $"/api/admin/roles/{roleId}/permissions/{permissionId}";

        public static string Permission(int permissionId) => $"/api/admin/permissions/{permissionId}";
    }
}
