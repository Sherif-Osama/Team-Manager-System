namespace TeamManager.Domain.Common.Events
{
    public sealed record TaskCompletedDomainEvent(long TaskId, Guid AssigneeUserId) : IDomainEvent;
}