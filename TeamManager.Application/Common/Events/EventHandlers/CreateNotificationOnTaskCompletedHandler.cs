using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateNotificationOnTaskCompletedHandler(INotificationRepository notificationRepository)
        : INotificationHandler<DomainEventNotification<TaskStatusChangedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskStatusChangedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            if (domainEvent.ToStatus != TaskItemStatus.Done || domainEvent.Task.AssigneeUserId == null)
                return;

            var preferences = await notificationRepository.GetPreferencesByUserIdAsync(domainEvent.Task.AssigneeUserId.Value, cancellationToken);

            var preference = preferences.FirstOrDefault(x => x.NotificationType == NotificationType.TaskCompleted);

            if (preference is not null && !preference.IsEnabled)
                return;

            var newNotification = new Notification(domainEvent.Task.AssigneeUserId.Value, NotificationType.TaskCompleted, "Task Completed",
                "A task assigned to you has been completed.", "Task", domainEvent.Task.Id.ToString());

            await notificationRepository.AddNotificationAsync(newNotification, cancellationToken);
        }
    }
}