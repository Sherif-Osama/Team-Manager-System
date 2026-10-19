using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events.Teams
{
    public record TeamMemberAddedDomainEvent(TeamMember Member) : IDomainEvent;
}
