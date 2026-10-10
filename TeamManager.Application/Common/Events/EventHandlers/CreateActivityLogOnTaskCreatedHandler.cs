using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnTaskCreatedHandler(IActivityLogRepository activityLogRepository,
        IProjectRepository projectRepository) : INotificationHandler<DomainEventNotification<TaskCreatedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskCreatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var task = notification.DomainEvent.Task;

            var project = await projectRepository.GetByIdAsync(task.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(task.ProjectId);

            var metadata = JsonSerializer.Serialize(new
            {
                taskTitle = task.Title
            });

            var activityLog = new Domain.Entities.ActivityLog(project.TeamId, task.CreatedBy, ActivityTypes.TaskCreated,
                "Task", task.Id.ToString(), task.ProjectId, metadata);

            await activityLogRepository.AddAsync(activityLog, cancellationToken);
        }
    }
}