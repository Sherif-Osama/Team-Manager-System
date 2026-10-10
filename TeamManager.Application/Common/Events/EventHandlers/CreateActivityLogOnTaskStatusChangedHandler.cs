using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnTaskStatusChangedHandler(IActivityLogRepository activityLogRepository, IProjectRepository projectRepository)
        : INotificationHandler<DomainEventNotification<TaskStatusChangedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskStatusChangedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;
            var task = domainEvent.Task;

            var project = await projectRepository.GetByIdAsync(task.ProjectId, cancellationToken);

            if (project is null)
                throw new InvalidOperationException($"Project {task.ProjectId} was not found while creating the activity log.");

            var metadata = JsonSerializer.Serialize(new
            {
                from = domainEvent.FromStatus.ToString(),
                to = domainEvent.ToStatus.ToString()
            });

            var activityLog = new Domain.Entities.ActivityLog(project.TeamId, domainEvent.ActorUserId, ActivityTypes.TaskStatusChanged,
                entityType: "Task", task.Id.ToString(), task.ProjectId, metadata);

            await activityLogRepository.AddAsync(activityLog, cancellationToken);
        }
    }
}