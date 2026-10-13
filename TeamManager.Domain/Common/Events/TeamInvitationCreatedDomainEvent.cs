using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events
{

    public sealed record TeamInvitationCreatedDomainEvent(TeamInvitation Invitation) : IDomainEvent;
}