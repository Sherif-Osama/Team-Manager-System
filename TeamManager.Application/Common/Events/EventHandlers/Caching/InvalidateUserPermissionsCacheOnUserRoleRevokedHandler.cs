using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateUserPermissionsCacheOnUserRoleRevokedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<UserRoleRevokedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserRoleRevokedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.UserPermissions(notification.DomainEvent.UserId));
            return Task.CompletedTask;
        }
    }
}
