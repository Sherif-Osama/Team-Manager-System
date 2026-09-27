namespace TeamManager.Domain.Common.Events
{
    public sealed record UserDeletedDomainEvent(Guid UserId, string Email) : IDomainEvent;
}