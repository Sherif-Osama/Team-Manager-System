namespace TeamManager.Domain.Common.Events
{
    public sealed record TaskAssignedDomainEvent(long TaskId, Guid AssignedUserId) : IDomainEvent;
}