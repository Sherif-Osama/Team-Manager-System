namespace TeamManager.Domain.Common.Events
{
    public sealed record TeamCreatedDomainEvent(Guid TeamId, Guid ActorUserId, string TeamName) : IDomainEvent;
}