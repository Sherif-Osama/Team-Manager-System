using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events
{
    public sealed record TaskAssignedDomainEvent(TaskItem Task, Guid AssignedUserId, Guid ActorUserId) : IDomainEvent;
}