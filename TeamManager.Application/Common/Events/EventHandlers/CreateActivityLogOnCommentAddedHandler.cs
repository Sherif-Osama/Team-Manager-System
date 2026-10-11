using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnCommentAddedHandler(IActivityLogRepository activityLogRepository,
        IProjectRepository projectRepository) : INotificationHandler<DomainEventNotification<CommentAddedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<CommentAddedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var task = domainEvent.Task;
            var comment = domainEvent.Comment;

            var project = await projectRepository.GetByIdAsync(task.ProjectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(task.ProjectId);

            var metadata = JsonSerializer.Serialize(new
            {
                taskId = task.Id,
                commentId = comment.Id
            });

            var activityLog = new Domain.Entities.ActivityLog(project.TeamId, domainEvent.AuthorUserId, ActivityTypes.CommentAdded,
                "TaskComment", comment.Id.ToString(), task.ProjectId, metadata);

            await activityLogRepository.AddAsync(activityLog, cancellationToken);
        }
    }
}