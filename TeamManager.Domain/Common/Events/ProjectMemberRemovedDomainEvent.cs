namespace TeamManager.Domain.Common.Events
{
    public sealed record ProjectMemberRemovedDomainEvent(Guid ProjectId, long MemberId, Guid RemovedUserId, Guid ActorUserId) : IDomainEvent;
}