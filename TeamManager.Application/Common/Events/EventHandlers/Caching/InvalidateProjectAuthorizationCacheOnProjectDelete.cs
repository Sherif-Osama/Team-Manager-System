using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Projects;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateProjectAuthorizationCacheOnProjectDelete(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<ProjectDeletedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<ProjectDeletedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.ProjectAuthorization(notification.DomainEvent.projectId));
        }
    }
}