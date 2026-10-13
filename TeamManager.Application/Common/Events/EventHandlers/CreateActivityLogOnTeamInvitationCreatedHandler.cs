using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnTeamInvitationCreatedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<TeamInvitationCreatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamInvitationCreatedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var invitation = notification.DomainEvent.Invitation;

            var metadata = JsonSerializer.Serialize(new
            {
                invitedEmail = invitation.InvitedEmail,
                teamRole = invitation.TeamRole.ToString()
            });

            return activityLogWriter.WriteTeamActivityAsync(teamId: invitation.TeamId, actorUserId: invitation.InvitedBy,
                activityType: ActivityTypes.TeamInvitationCreated, entityType: "TeamInvitation", entityId: invitation.Id.ToString(),
                metadata: metadata, cancellationToken: cancellationToken);
        }
    }
}