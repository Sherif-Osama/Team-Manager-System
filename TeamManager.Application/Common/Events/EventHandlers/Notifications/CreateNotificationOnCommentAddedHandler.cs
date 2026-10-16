using MediatR;
using TeamManager.Application.Abstractions.Notifications;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events.Comments;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Events.EventHandlers.Notifications
{
    public sealed class CreateNotificationOnCommentAddedHandler(INotificationWriter notificationWriter)
        : INotificationHandler<DomainEventNotification<CommentAddedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<CommentAddedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            if (domainEvent.AssigneeUserId is not Guid assigneeUserId || domainEvent.AuthorUserId == assigneeUserId)
                return;

            if (domainEvent.Comment.Mentions.Any(mention => mention.MentionedUserId == assigneeUserId))
                return;

            await notificationWriter.CreateIfEnabledAsync(assigneeUserId, NotificationType.CommentAdded, "New Comment",
                "A new comment was added to your task.", EntityType.TaskComment, domainEvent.Comment.Id.ToString(), cancellationToken);
        }
    }
}
