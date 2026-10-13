using TeamManager.Domain.Enums;

namespace TeamManager.Domain.Common.Events
{
    public sealed record ProjectMemberRoleChangedDomainEvent(Guid ProjectId, long MemberId, Guid MemberUserId, Guid ActorUserId,
        ProjectRole OldRole, ProjectRole NewRole) : IDomainEvent;
}