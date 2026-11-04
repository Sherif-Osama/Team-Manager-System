namespace TeamManager.Domain.Common.Events.Projects
{
    public record ProjectDeletedDomainEvent(Guid projectId) : IDomainEvent;
}