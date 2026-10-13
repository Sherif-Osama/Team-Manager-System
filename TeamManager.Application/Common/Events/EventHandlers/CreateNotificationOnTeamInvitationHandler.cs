using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateNotificationOnTeamInvitationHandler(INotificationRepository notificationRepository)
        : INotificationHandler<DomainEventNotification<TeamInvitationCreatedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TeamInvitationCreatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var invitation = notification.DomainEvent.Invitation;

            if (invitation.InvitedUserId is not Guid invitedUserId || invitedUserId == Guid.Empty)
                return;

            var preferences = await notificationRepository.GetPreferencesByUserIdAsync(invitedUserId, cancellationToken);

            var preference = preferences.FirstOrDefault(x => x.NotificationType == NotificationType.TeamInvitation);

            if (preference is not null && !preference.IsEnabled)
                return;

            var newNotification = new Notification(invitedUserId, NotificationType.TeamInvitation,
                "New Team Invitation", "You have received a new team invitation.", "TeamInvitation", invitation.Id.ToString());

            await notificationRepository.AddNotificationAsync(newNotification, cancellationToken);
        }
    }
}