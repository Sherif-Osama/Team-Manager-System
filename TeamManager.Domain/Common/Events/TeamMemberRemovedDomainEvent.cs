namespace TeamManager.Domain.Common.Events
{
    public sealed record TeamMemberRemovedDomainEvent(Guid TeamId, Guid RemovedUserId) : IDomainEvent;
}