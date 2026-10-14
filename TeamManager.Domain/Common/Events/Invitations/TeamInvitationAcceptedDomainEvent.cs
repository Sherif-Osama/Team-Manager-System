using TeamManager.Domain.Enums;

namespace TeamManager.Domain.Common.Events.Invitations
{
    public sealed record TeamInvitationAcceptedDomainEvent(Guid TeamId, Guid InvitationId, Guid ActorUserId,
        string InvitedEmail, TeamRole TeamRole) : IDomainEvent;
}
