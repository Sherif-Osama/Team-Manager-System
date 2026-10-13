using MediatR;
using TeamManager.Application.Abstractions.Notifications;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateNotificationOnCommentMentionedHandler(INotificationWriter notificationWriter)
        : INotificationHandler<DomainEventNotification<CommentMentionedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<CommentMentionedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            await notificationWriter.CreateIfEnabledAsync(domainEvent.MentionedUserId, NotificationType.Mention, "You were mentioned",
                "You were mentioned in a task comment.", EntityType.TaskComment, domainEvent.Comment.Id.ToString(), cancellationToken);
        }
    }
}