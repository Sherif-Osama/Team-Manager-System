using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Teams;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateUserAuthorizationCacheOnTeamMemberAddedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<TeamMemberAddedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamMemberAddedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.Member.UserId));
            return Task.CompletedTask;
        }
    }
}
