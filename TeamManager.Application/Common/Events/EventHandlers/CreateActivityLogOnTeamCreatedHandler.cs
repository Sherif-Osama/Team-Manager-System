using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnTeamCreatedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<TeamCreatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamCreatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var metadata = JsonSerializer.Serialize(new
            {
                teamName = domainEvent.TeamName
            });

            return activityLogWriter.WriteTeamActivityAsync(domainEvent.TeamId, domainEvent.ActorUserId, ActivityTypes.TeamCreated,
                "Team", domainEvent.TeamId.ToString(), metadata, cancellationToken);
        }
    }
}