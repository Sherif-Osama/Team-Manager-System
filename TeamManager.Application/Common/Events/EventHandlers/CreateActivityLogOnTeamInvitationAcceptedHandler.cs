using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnTeamInvitationAcceptedHandler(IActivityLogWriter activityLogWriter)
       : INotificationHandler<DomainEventNotification<TeamInvitationAcceptedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamInvitationAcceptedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var metadata = JsonSerializer.Serialize(new
            {
                invitedEmail = domainEvent.InvitedEmail,
                teamRole = domainEvent.TeamRole.ToString()
            });

            return activityLogWriter.WriteTeamActivityAsync(domainEvent.TeamId, domainEvent.ActorUserId,
                ActivityTypes.TeamInvitationAccepted, "TeamInvitation", domainEvent.InvitationId.ToString(), metadata,
                cancellationToken);
        }
    }
}