using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events.Projects
{
    public record ProjectMemberAddedDomainEvent(ProjectMember member) : IDomainEvent;
}