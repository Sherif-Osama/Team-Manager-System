namespace TeamManager.Domain.Common.Events.Roles
{

    public sealed record RolePermissionAddedDomainEvent(int RoleId, int PermissionId) : IDomainEvent;
}
