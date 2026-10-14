namespace TeamManager.Domain.Common.Events.Users
{

    public sealed record UserRoleAssignedDomainEvent(Guid UserId, int RoleId) : IDomainEvent;
}