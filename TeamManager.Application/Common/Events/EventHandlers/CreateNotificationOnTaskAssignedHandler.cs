using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateNotificationOnTaskAssignedHandler(INotificationRepository notificationRepository)
        : INotificationHandler<DomainEventNotification<TaskAssignedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskAssignedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            if (domainEvent.ActorUserId == domainEvent.AssignedUserId)
                return;

            var preferences = await notificationRepository.GetPreferencesByUserIdAsync(domainEvent.AssignedUserId, cancellationToken);

            var preference = preferences.FirstOrDefault(x => x.NotificationType == NotificationType.TaskAssigned);

            if (preference is not null && !preference.IsEnabled)
                return;

            var newNotification = new Notification(domainEvent.AssignedUserId,
                NotificationType.TaskAssigned, "Task Assigned", "You have been assigned to a task.",
                "Task", domainEvent.Task.Id.ToString());

            await notificationRepository.AddNotificationAsync(newNotification, cancellationToken);
        }
    }
}