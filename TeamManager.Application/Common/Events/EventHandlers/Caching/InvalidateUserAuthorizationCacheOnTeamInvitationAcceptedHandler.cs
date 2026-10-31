using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Invitations;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateUserAuthorizationCacheOnTeamInvitationAcceptedHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<TeamInvitationAcceptedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamInvitationAcceptedDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.ActorUserId));
            return Task.CompletedTask;
        }
    }
}
