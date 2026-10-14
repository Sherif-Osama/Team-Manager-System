using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events.Teams;

namespace TeamManager.Application.Common.Events.EventHandlers.ActivityLog
{
    public sealed class CreateActivityLogOnTeamMemberRemovedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<TeamMemberRemovedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamMemberRemovedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var metadata = JsonSerializer.Serialize(new
            {
                removedUserId = domainEvent.RemovedUserId
            });

            return activityLogWriter.WriteTeamActivityAsync(domainEvent.TeamId, domainEvent.ActorUserId,
                ActivityTypes.TeamMemberRemoved, EntityType.TeamMember, domainEvent.MemberId.ToString(), metadata, cancellationToken);
        }
    }
}