using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Common.Events.Invitations;
using TeamManager.Domain.Common.Events.Projects;
using TeamManager.Domain.Common.Events.Teams;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Application.Common.Events.EventHandlers.Caching
{
    public sealed class InvalidateAuthorizationCacheHandler(IPostCommitCacheInvalidationDispatcher dispatcher)
        : INotificationHandler<DomainEventNotification<TeamMemberRemovedDomainEvent>>,
        INotificationHandler<DomainEventNotification<TeamMemberRoleChangedDomainEvent>>,
        INotificationHandler<DomainEventNotification<TeamInvitationAcceptedDomainEvent>>,
        INotificationHandler<DomainEventNotification<TeamOwnershipTransferredDomainEvent>>,
        INotificationHandler<DomainEventNotification<ProjectMemberRemovedDomainEvent>>,
        INotificationHandler<DomainEventNotification<ProjectMemberRoleChangedDomainEvent>>,
        INotificationHandler<DomainEventNotification<ProjectOwnershipTransferredDomainEvent>>,
        INotificationHandler<DomainEventNotification<UserDeactivatedDomainEvent>>,
        INotificationHandler<DomainEventNotification<UserReactivatedDomainEvent>>,
        INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>,
        INotificationHandler<DomainEventNotification<TeamMemberAddedDomainEvent>>,
        INotificationHandler<DomainEventNotification<ProjectMemberAddedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamMemberRemovedDomainEvent> notification, CancellationToken cancellationToken)
            => InvalidateTeamMembership(notification.DomainEvent.TeamId, notification.DomainEvent.RemovedUserId);

        public Task Handle(DomainEventNotification<TeamMemberRoleChangedDomainEvent> notification, CancellationToken cancellationToken)
            => InvalidateTeamMembership(notification.DomainEvent.TeamId, notification.DomainEvent.MemberUserId);

        public Task Handle(DomainEventNotification<TeamInvitationAcceptedDomainEvent> notification, CancellationToken cancellationToken)
            => InvalidateTeamMembership(notification.DomainEvent.TeamId, notification.DomainEvent.ActorUserId);

        public Task Handle(DomainEventNotification<TeamOwnershipTransferredDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.TeamAuthorization(notification.DomainEvent.TeamId));
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.PreviousOwnerUserId));
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.NewOwnerUserId));
            return Task.CompletedTask;
        }

        public Task Handle(DomainEventNotification<ProjectMemberRemovedDomainEvent> notification, CancellationToken cancellationToken)
            => InvalidateProjectMembership(notification.DomainEvent.ProjectId, notification.DomainEvent.RemovedUserId);

        public Task Handle(DomainEventNotification<ProjectMemberRoleChangedDomainEvent> notification, CancellationToken cancellationToken)
            => InvalidateProjectMembership(notification.DomainEvent.ProjectId, notification.DomainEvent.MemberUserId);

        public Task Handle(DomainEventNotification<ProjectOwnershipTransferredDomainEvent> notification, CancellationToken cancellationToken)
        {
            dispatcher.EnqueueTag(CacheTags.ProjectAuthorization(notification.DomainEvent.ProjectId));
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.PreviousOwnerUserId));
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(notification.DomainEvent.NewOwnerUserId));
            return Task.CompletedTask;
        }

        public Task Handle(DomainEventNotification<UserDeactivatedDomainEvent> notification, CancellationToken cancellationToken)
            => InvalidateUserMemberships(notification.DomainEvent.UserId);

        public Task Handle(DomainEventNotification<UserReactivatedDomainEvent> notification, CancellationToken cancellationToken)
            => InvalidateUserMemberships(notification.DomainEvent.UserId);

        public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken cancellationToken)
            => InvalidateUserMemberships(notification.DomainEvent.UserId);

        public Task Handle(DomainEventNotification<TeamMemberAddedDomainEvent> notification, CancellationToken cancellationToken)
            => InvalidateTeamMembership(notification.DomainEvent.Member.TeamId, notification.DomainEvent.Member.UserId);

        public Task Handle(DomainEventNotification<ProjectMemberAddedDomainEvent> notification, CancellationToken cancellationToken)
        => InvalidateProjectMembership(notification.DomainEvent.member.ProjectId, notification.DomainEvent.member.UserId);

        private Task InvalidateTeamMembership(Guid teamId, Guid userId)
        {
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(userId));
            return Task.CompletedTask;
        }

        private Task InvalidateProjectMembership(Guid projectId, Guid userId)
        {
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(userId));
            return Task.CompletedTask;
        }

        private Task InvalidateUserMemberships(Guid userId)
        {
            dispatcher.EnqueueTag(CacheTags.UserAuthorization(userId));
            return Task.CompletedTask;
        }
    }
}
