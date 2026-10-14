namespace TeamManager.Domain.Common.Events.Users
{
    public sealed record UserDeletedDomainEvent(Guid UserId, string Email) : IDomainEvent;
}