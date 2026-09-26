using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class SuspendProjectMembershipsOnUserDeactivatedHandler(IProjectRepository projectRepository)
        : INotificationHandler<DomainEventNotification<UserDeactivatedDomainEvent>>
    {
        public async Task Handle(DomainEventNotification<UserDeactivatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            await projectRepository.DeactivateOwnedProjectsAsync(notification.DomainEvent.UserId, cancellationToken);
            await projectRepository.SuspendActiveMembershipsAsync(notification.DomainEvent.UserId, cancellationToken);
        }
    }
}