using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateNotificationOnTaskCompletedHandler(INotificationRepository notificationRepository)
        : INotificationHandler<DomainEventNotification<TaskCompletedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskCompletedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var preferences = await notificationRepository.GetPreferencesByUserIdAsync(domainEvent.AssigneeUserId, cancellationToken);

            var preference = preferences.FirstOrDefault(x => x.NotificationType == NotificationType.TaskCompleted);

            if (preference is not null && !preference.IsEnabled)
                return;

            var newNotification = new Notification(domainEvent.AssigneeUserId, NotificationType.TaskCompleted, "Task Completed",
                "A task assigned to you has been completed.", "Task", domainEvent.TaskId.ToString());

            await notificationRepository.AddNotificationAsync(newNotification, cancellationToken);
        }
    }
}