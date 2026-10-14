using TeamManager.Domain.Enums;

namespace TeamManager.Domain.Common.Events.Teams
{
    public sealed record TeamMemberRoleChangedDomainEvent(Guid TeamId, long MemberId, Guid MemberUserId, Guid ActorUserId, TeamRole OldRole, TeamRole NewRole) : IDomainEvent;
}