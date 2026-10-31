using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Teams;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateUserAuthorizationCacheOnTeamMemberRemovedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<TeamMemberRemovedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamMemberRemovedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.RemovedUserId));
            return Task.CompletedTask;
        }
    }
}
