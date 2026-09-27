namespace TeamManager.Domain.Common.Events
{
    public sealed record UserReactivatedDomainEvent(Guid UserId, string Email) : IDomainEvent;
}