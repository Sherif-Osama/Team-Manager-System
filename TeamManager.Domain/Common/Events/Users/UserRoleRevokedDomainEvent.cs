namespace TeamManager.Domain.Common.Events.Users
{
    public sealed record UserRoleRevokedDomainEvent(Guid UserId, int RoleId) : IDomainEvent;
}