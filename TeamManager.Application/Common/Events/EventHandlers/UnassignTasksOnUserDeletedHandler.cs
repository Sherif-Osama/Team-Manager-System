using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class UnassignTasksOnUserDeletedHandler(ITaskRepository taskRepository)
        : INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken cancellationToken)
            => taskRepository.UnassignActiveTasksAsync(notification.DomainEvent.UserId, cancellationToken);
    }
}