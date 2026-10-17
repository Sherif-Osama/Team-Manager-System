using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Comments;
using TeamManager.Domain.Common.Events.Tasks;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateTaskCacheHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<TaskChangedDomainEvent>>,
          INotificationHandler<DomainEventNotification<TaskChecklistChangedDomainEvent>>,
          INotificationHandler<DomainEventNotification<TaskAttachmentsChangedDomainEvent>>,
          INotificationHandler<DomainEventNotification<TaskCommentsChangedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TaskChangedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.Task(notification.DomainEvent.TaskId));

            return Task.CompletedTask;
        }

        public Task Handle(DomainEventNotification<TaskChecklistChangedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.TaskChecklist(notification.DomainEvent.TaskId));

            return Task.CompletedTask;
        }

        public Task Handle(DomainEventNotification<TaskAttachmentsChangedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.TaskAttachments(notification.DomainEvent.TaskId));

            return Task.CompletedTask;
        }

        public Task Handle(DomainEventNotification<TaskCommentsChangedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.TaskComments(notification.DomainEvent.TaskId));

            return Task.CompletedTask;
        }
    }
}