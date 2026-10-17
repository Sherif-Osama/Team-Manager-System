namespace TeamManager.Domain.Common.Events.Roles
{

    public sealed record RolePermissionRemovedDomainEvent(int RoleId, int PermissionId) : IDomainEvent;
}
