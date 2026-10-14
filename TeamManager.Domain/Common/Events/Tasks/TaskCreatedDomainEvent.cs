using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events.Tasks
{
    public sealed record TaskCreatedDomainEvent(TaskItem Task) : IDomainEvent;
}