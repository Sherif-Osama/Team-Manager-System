using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Roles;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateAllUserPermissionsCacheOnRolePermissionAddedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<RolePermissionAddedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<RolePermissionAddedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.AllUserPermissions);
            return Task.CompletedTask;
        }
    }
}
