namespace TeamManager.Domain.Common.Events
{

    public sealed record TeamInvitationCreatedDomainEvent(Guid InvitationId, Guid TeamId, Guid InvitedUserId) : IDomainEvent;
}