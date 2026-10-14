using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events.Tasks;

namespace TeamManager.Application.Common.Events.EventHandlers.ActivityLog
{
    public sealed class CreateActivityLogOnTaskStatusChangedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<TaskStatusChangedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskStatusChangedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;
            var task = domainEvent.Task;

            var metadata = JsonSerializer.Serialize(new
            {
                from = domainEvent.FromStatus.ToString(),
                to = domainEvent.ToStatus.ToString()
            });

            await activityLogWriter.WriteProjectActivityAsync(task.ProjectId, domainEvent.ActorUserId, ActivityTypes.TaskStatusChanged,
                EntityType.Task, task.Id.ToString(), metadata, cancellationToken);
        }
    }
}