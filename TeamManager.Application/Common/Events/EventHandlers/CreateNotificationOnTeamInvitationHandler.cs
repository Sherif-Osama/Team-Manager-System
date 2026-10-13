using MediatR;
using TeamManager.Application.Abstractions.Notifications;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateNotificationOnTeamInvitationHandler(INotificationWriter notificationWriter)
        : INotificationHandler<DomainEventNotification<TeamInvitationCreatedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TeamInvitationCreatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var invitation = notification.DomainEvent.Invitation;

            if (invitation.InvitedUserId is not Guid invitedUserId || invitedUserId == Guid.Empty)

                return;

            await notificationWriter.CreateIfEnabledAsync(invitedUserId, NotificationType.TeamInvitation, "New Team Invitation",
                "You have received a new team invitation.", EntityType.TeamInvitation, invitation.Id.ToString(), cancellationToken);
        }
    }
}