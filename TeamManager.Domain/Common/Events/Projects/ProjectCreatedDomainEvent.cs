namespace TeamManager.Domain.Common.Events.Projects
{
    public sealed record ProjectCreatedDomainEvent(Guid ProjectId, Guid ActorUserId, string ProjectName) : IDomainEvent;
}