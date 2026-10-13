using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.Logging;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnProjectCreatedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<ProjectCreatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<ProjectCreatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var metadata = JsonSerializer.Serialize(new
            {
                projectName = domainEvent.ProjectName
            });

            return activityLogWriter.WriteProjectActivityAsync(domainEvent.ProjectId, domainEvent.ActorUserId,
                ActivityTypes.ProjectCreated, LogEntityTypes.Project, domainEvent.ProjectId.ToString(), metadata, cancellationToken);
        }
    }
}