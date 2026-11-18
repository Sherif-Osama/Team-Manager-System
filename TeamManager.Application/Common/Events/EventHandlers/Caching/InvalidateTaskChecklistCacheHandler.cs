using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Tasks;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateTaskChecklistCacheHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<TaskChecklistChangedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TaskChecklistChangedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.TaskChecklist(notification.DomainEvent.TaskId));
            return Task.CompletedTask;
        }
    }
}