using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnTaskAssignedHandler(IActivityLogRepository activityLogRepository,
        IProjectRepository projectRepository) : INotificationHandler<DomainEventNotification<TaskAssignedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskAssignedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;
            var task = domainEvent.Task;

            var project = await projectRepository.GetByIdAsync(task.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(task.ProjectId);

            var metadata = JsonSerializer.Serialize(new
            {
                previousAssigneeUserId = domainEvent.PreviousAssigneeUserId,
                assignedUserId = domainEvent.AssignedUserId
            });

            var activityLog = new Domain.Entities.ActivityLog(project.TeamId, domainEvent.ActorUserId,
                ActivityTypes.TaskAssigned, "Task", entityId: task.Id.ToString(), task.ProjectId, metadata);

            await activityLogRepository.AddAsync(activityLog, cancellationToken);
        }
    }
}