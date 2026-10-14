using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events.Tasks
{
    public sealed record TaskAssignedDomainEvent(TaskItem Task, Guid? PreviousAssigneeUserId, Guid AssignedUserId, Guid ActorUserId) : IDomainEvent;
}