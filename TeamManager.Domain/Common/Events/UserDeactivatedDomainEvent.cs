namespace TeamManager.Domain.Common.Events
{
    public sealed record UserDeactivatedDomainEvent(Guid UserId, string Email) : IDomainEvent;
}