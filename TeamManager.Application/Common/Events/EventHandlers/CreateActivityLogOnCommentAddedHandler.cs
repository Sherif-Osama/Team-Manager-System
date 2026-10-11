using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnCommentAddedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<CommentAddedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<CommentAddedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var task = domainEvent.Task;
            var comment = domainEvent.Comment;

            var metadata = JsonSerializer.Serialize(new
            {
                taskId = task.Id,
                commentId = comment.Id
            });

            await activityLogWriter.WriteProjectActivityAsync(task.ProjectId, domainEvent.AuthorUserId, ActivityTypes.CommentAdded,
                "TaskComment", comment.Id.ToString(), metadata, cancellationToken);
        }
    }
}