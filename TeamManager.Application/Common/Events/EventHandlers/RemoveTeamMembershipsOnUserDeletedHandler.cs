using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class RemoveTeamMembershipsOnUserDeletedHandler(ITeamRepository teamRepository)
        : INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken ct) =>
            teamRepository.RemoveActiveMembershipsAsync(notification.DomainEvent.UserId, ct);
    }
}