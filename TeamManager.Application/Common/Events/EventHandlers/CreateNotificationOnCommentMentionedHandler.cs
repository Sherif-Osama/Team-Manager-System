using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateNotificationOnCommentMentionedHandler(INotificationRepository notificationRepository)
        : INotificationHandler<DomainEventNotification<CommentMentionedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<CommentMentionedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var preferences = await notificationRepository.GetPreferencesByUserIdAsync(domainEvent.MentionedUserId, cancellationToken);

            var preference = preferences.FirstOrDefault(x => x.NotificationType == NotificationType.Mention);

            if (preference is not null && !preference.IsEnabled)
                return;

            var comment = domainEvent.Comment;

            var newNotification = new Notification(domainEvent.MentionedUserId, NotificationType.Mention, "You were mentioned",
                "You were mentioned in a task comment.", "TaskComment", comment.Id.ToString());

            await notificationRepository.AddNotificationAsync(newNotification, cancellationToken);
        }
    }
}