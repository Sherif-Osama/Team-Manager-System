using TeamManager.Domain.Enums;

namespace TeamManager.Domain.Common.Events
{
    public sealed record TeamInvitationRejectedDomainEvent(Guid TeamId, Guid InvitationId, Guid ActorUserId, string InvitedEmail,
        TeamRole TeamRole) : IDomainEvent;
}