using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateUserPermissionsCacheHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<UserRoleAssignedDomainEvent>>, INotificationHandler<DomainEventNotification<UserRoleRevokedDomainEvent>>,
          INotificationHandler<DomainEventNotification<UserDeactivatedDomainEvent>>,
          INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>,
          INotificationHandler<DomainEventNotification<UserReactivatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserRoleAssignedDomainEvent> notification, CancellationToken cancellationToken)
            => EnqueueInvalidation(notification.DomainEvent.UserId);

        public Task Handle(DomainEventNotification<UserRoleRevokedDomainEvent> notification, CancellationToken cancellationToken) => EnqueueInvalidation(notification.DomainEvent.UserId);

        public Task Handle(DomainEventNotification<UserDeactivatedDomainEvent> notification, CancellationToken cancellationToken)
            => EnqueueInvalidation(notification.DomainEvent.UserId);

        public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken cancellationToken) => EnqueueInvalidation(notification.DomainEvent.UserId);

        public Task Handle(DomainEventNotification<UserReactivatedDomainEvent> notification, CancellationToken cancellationToken) => EnqueueInvalidation(notification.DomainEvent.UserId);

        private Task EnqueueInvalidation(Guid userId)
        {
            dispatcher.EnqueueTag(CacheTags.UserPermissions(userId));
            return Task.CompletedTask;
        }
    }
}