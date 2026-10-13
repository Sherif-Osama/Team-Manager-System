namespace TeamManager.Domain.Common.Events
{
    public sealed record TeamMemberRemovedDomainEvent(Guid TeamId, long MemberId, Guid RemovedUserId, Guid ActorUserId) : IDomainEvent;
}