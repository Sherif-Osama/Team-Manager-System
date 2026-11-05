using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Teams;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateTeamAuthorizationCacheOnTeamDeleteHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<TeamDeletedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamDeletedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.TeamAuthorization(notification.DomainEvent.teamId));
            return Task.CompletedTask;
        }
    }
}