namespace TeamManager.Application.Common.AuditLog
{
    public static class AuditLogActions
    {
        public const string UserRoleAssigned = "user.role.assigned";
        public const string UserRoleRevoked = "user.role.revoked";
        public const string UserDeactivated = "user.account.deactivated";
        public const string UserDeleted = "user.account.deleted";
    }
}