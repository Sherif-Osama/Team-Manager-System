namespace TeamManager.Domain.Common.Events.Tasks
{
    public sealed record TaskChecklistChangedDomainEvent(long TaskId) : IDomainEvent;
}