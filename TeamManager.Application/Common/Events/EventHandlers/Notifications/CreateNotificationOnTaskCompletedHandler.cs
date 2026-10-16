using MediatR;
using TeamManager.Application.Abstractions.Notifications;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events.Tasks;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Events.EventHandlers.Notifications
{
    public sealed class CreateNotificationOnTaskCompletedHandler(INotificationWriter notificationWriter)
        : INotificationHandler<DomainEventNotification<TaskStatusChangedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskStatusChangedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            if (domainEvent.ToStatus != TaskItemStatus.Done || domainEvent.ActorUserId == domainEvent.Task.CreatedBy)
                return;

            await notificationWriter.CreateIfEnabledAsync(domainEvent.Task.CreatedBy, NotificationType.TaskCompleted, "Task Completed",
                "A task you created has been completed.", EntityType.Task, domainEvent.Task.Id.ToString(), cancellationToken);
        }
    }
}
