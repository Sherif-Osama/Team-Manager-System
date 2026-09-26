using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class ReactivateProjectMembershipsOnUserReactivatedHandler(IProjectRepository projectRepository)
        : INotificationHandler<DomainEventNotification<UserReactivatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserReactivatedDomainEvent> notification, CancellationToken cancellationToken)
            => projectRepository.ReactivateSuspendedMembershipsAsync(notification.DomainEvent.UserId, cancellationToken);
    }
}