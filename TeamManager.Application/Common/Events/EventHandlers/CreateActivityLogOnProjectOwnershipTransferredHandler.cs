using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.Logging;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnProjectOwnershipTransferredHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<ProjectOwnershipTransferredDomainEvent>>
    {
        public Task Handle(DomainEventNotification<ProjectOwnershipTransferredDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var metadata = JsonSerializer.Serialize(new
            {
                previousOwnerUserId = domainEvent.PreviousOwnerUserId,
                newOwnerUserId = domainEvent.NewOwnerUserId
            });

            return activityLogWriter.WriteProjectActivityAsync(domainEvent.ProjectId, domainEvent.ActorUserId,
                ActivityTypes.ProjectOwnershipTransferred, LogEntityTypes.Project, domainEvent.ProjectId.ToString(), metadata,
                cancellationToken);
        }
    }
}
