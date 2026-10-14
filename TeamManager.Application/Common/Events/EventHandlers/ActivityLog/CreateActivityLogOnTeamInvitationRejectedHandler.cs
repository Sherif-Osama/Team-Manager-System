using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events.Invitations;

namespace TeamManager.Application.Common.Events.EventHandlers.ActivityLog
{
    public sealed class CreateActivityLogOnTeamInvitationRejectedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<TeamInvitationRejectedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamInvitationRejectedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var metadata = JsonSerializer.Serialize(new
            {
                invitedEmail = domainEvent.InvitedEmail,
                teamRole = domainEvent.TeamRole.ToString()
            });

            return activityLogWriter.WriteTeamActivityAsync(domainEvent.TeamId, domainEvent.ActorUserId,
                ActivityTypes.TeamInvitationRejected, EntityType.TeamInvitation, domainEvent.InvitationId.ToString(), metadata,
                cancellationToken);
        }
    }
}