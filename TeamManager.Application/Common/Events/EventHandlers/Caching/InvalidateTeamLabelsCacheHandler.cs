using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Teams;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateTeamLabelsCacheHandler(IPostCommitCacheInvalidationDispatcher dispatcher) : INotificationHandler<DomainEventNotification<TeamLabelsChangedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamLabelsChangedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.TeamLabels(notification.DomainEvent.TeamId));
            return Task.CompletedTask;
        }
    }
}