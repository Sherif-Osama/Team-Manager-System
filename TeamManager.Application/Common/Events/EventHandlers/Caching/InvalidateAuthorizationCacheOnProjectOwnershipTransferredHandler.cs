using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Projects;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateAuthorizationCacheOnProjectOwnershipTransferredHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<ProjectOwnershipTransferredDomainEvent>>
    {
        public Task Handle(DomainEventNotification<ProjectOwnershipTransferredDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.ProjectAuthorization(notification.DomainEvent.ProjectId));
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.PreviousOwnerUserId));
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.NewOwnerUserId));
            return Task.CompletedTask;
        }
    }
}
