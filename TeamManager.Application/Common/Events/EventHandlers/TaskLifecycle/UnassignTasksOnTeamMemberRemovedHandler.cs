using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events.Teams;

namespace TeamManager.Application.Common.Events.EventHandlers.TaskLifecycle
{
    public sealed class UnassignTasksOnTeamMemberRemovedHandler(ITaskRepository taskRepository)
        : INotificationHandler<DomainEventNotification<TeamMemberRemovedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamMemberRemovedDomainEvent> notification, CancellationToken cancellationToken)
        {
            return taskRepository.UnassignActiveTasksByTeamAsync(notification.DomainEvent.TeamId,
                notification.DomainEvent.RemovedUserId, cancellationToken);
        }
    }
}