using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnTaskAssignedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<TaskAssignedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskAssignedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;
            var task = domainEvent.Task;

            var metadata = JsonSerializer.Serialize(new
            {
                previousAssigneeUserId = domainEvent.PreviousAssigneeUserId,
                assignedUserId = domainEvent.AssignedUserId
            });

            await activityLogWriter.WriteProjectActivityAsync(task.ProjectId, domainEvent.ActorUserId, ActivityTypes.TaskAssigned,
                "Task", task.Id.ToString(), metadata, cancellationToken);
        }
    }
}