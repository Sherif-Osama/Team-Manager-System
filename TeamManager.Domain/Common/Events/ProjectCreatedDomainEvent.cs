namespace TeamManager.Domain.Common.Events
{
    public sealed record ProjectCreatedDomainEvent(Guid ProjectId, Guid ActorUserId, string ProjectName) : IDomainEvent;
}