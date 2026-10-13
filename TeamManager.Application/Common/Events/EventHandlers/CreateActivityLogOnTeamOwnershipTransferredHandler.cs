using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnTeamOwnershipTransferredHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<TeamOwnershipTransferredDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamOwnershipTransferredDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var metadata = JsonSerializer.Serialize(new
            {
                previousOwnerUserId = domainEvent.PreviousOwnerUserId,
                newOwnerUserId = domainEvent.NewOwnerUserId
            });

            return activityLogWriter.WriteTeamActivityAsync(domainEvent.TeamId, domainEvent.ActorUserId,
                ActivityTypes.TeamOwnershipTransferred, "Team", domainEvent.TeamId.ToString(), metadata, cancellationToken);
        }
    }
}