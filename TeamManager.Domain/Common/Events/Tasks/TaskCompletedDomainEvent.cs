using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Domain.Common.Events.Tasks
{
    public sealed record TaskStatusChangedDomainEvent(TaskItem Task, TaskItemStatus FromStatus, TaskItemStatus ToStatus, Guid ActorUserId) : IDomainEvent;
}