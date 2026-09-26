namespace TeamManager.Domain.Common.Events
{
    public sealed record ProjectMemberRemovedDomainEvent(Guid ProjectId, Guid RemovedUserId) : IDomainEvent;
}