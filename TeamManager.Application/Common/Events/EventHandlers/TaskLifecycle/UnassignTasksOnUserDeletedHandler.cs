using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Application.Common.Events.EventHandlers.TaskLifecycle
{
    public sealed class UnassignTasksOnUserDeletedHandler(ITaskRepository taskRepository)
        : INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken cancellationToken)
            => taskRepository.UnassignActiveTasksAsync(notification.DomainEvent.UserId, cancellationToken);
    }
}