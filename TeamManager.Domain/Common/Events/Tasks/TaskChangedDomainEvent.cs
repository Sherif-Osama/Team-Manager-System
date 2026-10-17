namespace TeamManager.Domain.Common.Events.Tasks
{
    public sealed record TaskChangedDomainEvent(long TaskId) : IDomainEvent;
}