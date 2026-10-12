namespace TeamManager.Domain.Common.Events
{
    public sealed record UserRoleRevokedDomainEvent(Guid UserId, int RoleId) : IDomainEvent;
}