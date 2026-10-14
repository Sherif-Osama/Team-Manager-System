using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Application.Common.Events.EventHandlers.MembershipLifecycle
{
    public sealed class RemoveProjectMembershipsOnUserDeletedHandler(IProjectRepository projectRepository)
        : INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken cancellationToken)
            => projectRepository.RemoveActiveMembershipsAsync(notification.DomainEvent.UserId, cancellationToken);
    }
}