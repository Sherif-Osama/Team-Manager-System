using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Projects;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateUserAuthorizationCacheOnProjectMemberRemovedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<ProjectMemberRemovedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<ProjectMemberRemovedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.RemovedUserId));
            return Task.CompletedTask;
        }
    }
}
