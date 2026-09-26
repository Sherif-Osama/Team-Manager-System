using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class SuspendTeamMembershipsOnUserDeactivatedHandler(ITeamRepository teamRepository) :
        INotificationHandler<DomainEventNotification<UserDeactivatedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<UserDeactivatedDomainEvent> notification, CancellationToken ct)
        {
            await teamRepository.DeactivateOwnedTeamsAsync(notification.DomainEvent.UserId, ct);
            await teamRepository.SuspendActiveMembershipsAsync(notification.DomainEvent.UserId, ct);
        }
    }
}