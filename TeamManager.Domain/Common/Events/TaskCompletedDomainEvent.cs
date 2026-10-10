using TeamManager.Domain.Enums;

namespace TeamManager.Domain.Common.Events
{
    public sealed record TaskStatusChangedDomainEvent(long TaskId, Guid? AssigneeUserId, TaskItemStatus FromStatus, TaskItemStatus ToStatus, Guid ActorUserId) : IDomainEvent;
}