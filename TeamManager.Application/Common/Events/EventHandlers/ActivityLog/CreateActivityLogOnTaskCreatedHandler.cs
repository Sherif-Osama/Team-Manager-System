using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events.Tasks;

namespace TeamManager.Application.Common.Events.EventHandlers.ActivityLog
{
    public sealed class CreateActivityLogOnTaskCreatedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<TaskCreatedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskCreatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var task = notification.DomainEvent.Task;

            var metadata = JsonSerializer.Serialize(new
            {
                taskTitle = task.Title
            });

            await activityLogWriter.WriteProjectActivityAsync(task.ProjectId, task.CreatedBy, ActivityTypes.TaskCreated,
                EntityType.Task, task.Id.ToString(), metadata, cancellationToken);
        }
    }
}