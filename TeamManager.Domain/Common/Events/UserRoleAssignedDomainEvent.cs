namespace TeamManager.Domain.Common.Events
{

    public sealed record UserRoleAssignedDomainEvent(Guid UserId, int RoleId) : IDomainEvent;
}