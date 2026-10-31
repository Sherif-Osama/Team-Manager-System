using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateUserCacheOnUserDeletedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.UserPermissions(notification.DomainEvent.UserId));
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.UserId));
            return Task.CompletedTask;
        }
    }
}
