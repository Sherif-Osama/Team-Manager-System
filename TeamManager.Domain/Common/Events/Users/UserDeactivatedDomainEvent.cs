namespace TeamManager.Domain.Common.Events.Users
{
    public sealed record UserDeactivatedDomainEvent(Guid UserId, string Email) : IDomainEvent;
}