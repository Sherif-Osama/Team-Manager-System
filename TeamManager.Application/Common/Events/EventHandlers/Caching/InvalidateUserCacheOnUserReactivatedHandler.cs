using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateUserCacheOnUserReactivatedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<UserReactivatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserReactivatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.UserPermissions(notification.DomainEvent.UserId));
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.UserId));
            return Task.CompletedTask;
        }
    }
}
