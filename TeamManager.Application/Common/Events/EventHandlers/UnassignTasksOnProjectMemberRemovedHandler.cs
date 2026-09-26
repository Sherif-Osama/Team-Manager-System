using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class UnassignTasksOnProjectMemberRemovedHandler(ITaskRepository taskRepository)
        : INotificationHandler<DomainEventNotification<ProjectMemberRemovedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<ProjectMemberRemovedDomainEvent> notification, CancellationToken cancellationToken)
        {
            return taskRepository.UnassignActiveTasksByProjectAsync(notification.DomainEvent.ProjectId,
                notification.DomainEvent.RemovedUserId, cancellationToken);
        }
    }
}
