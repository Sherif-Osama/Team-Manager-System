using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateUserPermissionsCacheOnUserRoleAssignedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<UserRoleAssignedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserRoleAssignedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.UserPermissions(notification.DomainEvent.UserId));
            return Task.CompletedTask;
        }
    }
}
