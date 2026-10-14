using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Common.Events.Teams;

namespace TeamManager.Application.Common.Events.EventHandlers.MembershipLifecycle
{
    public sealed class RemoveProjectMembershipsOnTeamMemberRemovedHandler(IProjectRepository projectRepository)
       : INotificationHandler<DomainEventNotification<TeamMemberRemovedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamMemberRemovedDomainEvent> notification, CancellationToken cancellationToken)
        {
            return projectRepository.RemoveMembershipsByTeamAsync(notification.DomainEvent.TeamId,
                notification.DomainEvent.RemovedUserId, cancellationToken);
        }
    }
}