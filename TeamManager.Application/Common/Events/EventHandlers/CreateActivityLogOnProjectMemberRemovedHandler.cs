using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnProjectMemberRemovedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<ProjectMemberRemovedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<ProjectMemberRemovedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var metadata = JsonSerializer.Serialize(new
            {
                removedUserId = domainEvent.RemovedUserId
            });

            return activityLogWriter.WriteProjectActivityAsync(domainEvent.ProjectId, domainEvent.ActorUserId,
                ActivityTypes.ProjectMemberRemoved, EntityType.ProjectMember, domainEvent.MemberId.ToString(), metadata, cancellationToken);
        }
    }
}
