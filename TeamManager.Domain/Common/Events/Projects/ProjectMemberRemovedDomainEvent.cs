namespace TeamManager.Domain.Common.Events.Projects
{
    public sealed record ProjectMemberRemovedDomainEvent(Guid ProjectId, long MemberId, Guid RemovedUserId, Guid ActorUserId) : IDomainEvent;
}