using MediatR;
using TeamManager.Application.Abstractions.Notifications;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events;
using TeamManager.Domain.Enums;
namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateNotificationOnTaskAssignedHandler(INotificationWriter notificationWriter)
        : INotificationHandler<DomainEventNotification<TaskAssignedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskAssignedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            if (domainEvent.ActorUserId == domainEvent.AssignedUserId)
                return;

            await notificationWriter.CreateIfEnabledAsync(domainEvent.AssignedUserId, NotificationType.TaskAssigned,
                "Task Assigned", "You have been assigned to a task.",
                EntityType.Task, domainEvent.Task.Id.ToString(), cancellationToken);
        }
    }
}