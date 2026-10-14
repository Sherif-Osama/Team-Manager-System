namespace TeamManager.Domain.Common.Events.Teams
{
    public sealed record TeamMemberRemovedDomainEvent(Guid TeamId, long MemberId, Guid RemovedUserId, Guid ActorUserId) : IDomainEvent;
}