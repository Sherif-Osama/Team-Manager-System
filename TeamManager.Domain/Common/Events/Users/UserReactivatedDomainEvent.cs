namespace TeamManager.Domain.Common.Events.Users
{
    public sealed record UserReactivatedDomainEvent(Guid UserId, string Email) : IDomainEvent;
}