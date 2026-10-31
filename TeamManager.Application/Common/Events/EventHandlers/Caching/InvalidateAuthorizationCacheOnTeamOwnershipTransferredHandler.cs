using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Teams;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateAuthorizationCacheOnTeamOwnershipTransferredHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<TeamOwnershipTransferredDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamOwnershipTransferredDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.TeamAuthorization(notification.DomainEvent.TeamId));
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.PreviousOwnerUserId));
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.NewOwnerUserId));
            return Task.CompletedTask;
        }
    }
}
