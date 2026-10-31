using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Projects;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateUserAuthorizationCacheOnProjectMemberRoleChangedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<ProjectMemberRoleChangedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<ProjectMemberRoleChangedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.MemberUserId));
            return Task.CompletedTask;
        }
    }
}
