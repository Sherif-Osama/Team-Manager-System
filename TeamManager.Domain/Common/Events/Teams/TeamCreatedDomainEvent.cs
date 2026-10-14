namespace TeamManager.Domain.Common.Events.Teams
{
    public sealed record TeamCreatedDomainEvent(Guid TeamId, Guid ActorUserId, string TeamName) : IDomainEvent;
}