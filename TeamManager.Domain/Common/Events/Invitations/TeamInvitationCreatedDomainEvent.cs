using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events.Invitations
{

    public sealed record TeamInvitationCreatedDomainEvent(TeamInvitation Invitation) : IDomainEvent;
}