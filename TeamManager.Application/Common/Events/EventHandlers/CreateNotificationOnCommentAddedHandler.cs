using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateNotificationOnCommentAddedHandler(INotificationRepository notificationRepository)
        : INotificationHandler<DomainEventNotification<CommentAddedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<CommentAddedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            if (domainEvent.AssigneeUserId.HasValue && domainEvent.AuthorUserId == domainEvent.AssigneeUserId.Value)
                return;

            if (!domainEvent.AssigneeUserId.HasValue)
                return;

            var preferences = await notificationRepository.GetPreferencesByUserIdAsync(domainEvent.AssigneeUserId.Value, cancellationToken);

            var preference = preferences.FirstOrDefault(x => x.NotificationType == NotificationType.CommentAdded);

            if (preference is not null && !preference.IsEnabled)
                return;

            var newNotification = new Notification(domainEvent.AssigneeUserId.Value, NotificationType.CommentAdded,
                "New Comment", "A new comment was added to your task.", "TaskComment", domainEvent.Comment.Id.ToString());

            await notificationRepository.AddNotificationAsync(newNotification, cancellationToken);
        }
    }
}