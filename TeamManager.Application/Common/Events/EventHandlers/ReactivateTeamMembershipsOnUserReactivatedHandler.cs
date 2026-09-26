using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class ReactivateTeamMembershipsOnUserReactivatedHandler(ITeamRepository teamRepository) :
        INotificationHandler<DomainEventNotification<UserReactivatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserReactivatedDomainEvent> notification, CancellationToken cancellationToken)
            => teamRepository.ReactivateSuspendedMembershipsAsync(notification.DomainEvent.UserId, cancellationToken);
    }
}