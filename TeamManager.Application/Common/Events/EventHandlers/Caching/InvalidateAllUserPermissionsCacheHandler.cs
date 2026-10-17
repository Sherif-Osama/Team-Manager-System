using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Roles;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateAllUserPermissionsCacheHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<RolePermissionAddedDomainEvent>>,
        INotificationHandler<DomainEventNotification<RolePermissionRemovedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<RolePermissionAddedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.AllUserPermissions);
            return Task.CompletedTask;
        }

        public Task Handle(DomainEventNotification<RolePermissionRemovedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.AllUserPermissions);
            return Task.CompletedTask;
        }
    }
}
