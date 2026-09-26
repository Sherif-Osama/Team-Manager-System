namespace TeamManager.Domain.Common.Events
{
    public sealed record UserDeletedDomainEvent(Guid UserId) : IDomainEvent;
}