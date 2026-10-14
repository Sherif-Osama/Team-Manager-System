using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Application.Common.Events.EventHandlers.MembershipLifecycle
{
    public sealed class ReactivateTeamMembershipsOnUserReactivatedHandler(ITeamRepository teamRepository) :
        INotificationHandler<DomainEventNotification<UserReactivatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserReactivatedDomainEvent> notification, CancellationToken cancellationToken)
            => teamRepository.ReactivateSuspendedMembershipsAsync(notification.DomainEvent.UserId, cancellationToken);
    }
}