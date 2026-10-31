using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Roles;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateAllUserPermissionsCacheOnRolePermissionRemovedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<RolePermissionRemovedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<RolePermissionRemovedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.AllUserPermissions);
            return Task.CompletedTask;
        }
    }
}
