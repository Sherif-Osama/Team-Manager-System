using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events
{
    public sealed record TaskCreatedDomainEvent(TaskItem Task) : IDomainEvent;
}