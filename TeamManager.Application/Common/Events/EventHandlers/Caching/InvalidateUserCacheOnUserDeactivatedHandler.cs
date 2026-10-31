using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateUserCacheOnUserDeactivatedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<UserDeactivatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserDeactivatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.UserPermissions(notification.DomainEvent.UserId));
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.UserId));
            return Task.CompletedTask;
        }
    }
}
