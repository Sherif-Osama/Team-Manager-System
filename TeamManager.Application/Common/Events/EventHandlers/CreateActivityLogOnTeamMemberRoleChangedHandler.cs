using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnTeamMemberRoleChangedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<TeamMemberRoleChangedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<TeamMemberRoleChangedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var metadata = JsonSerializer.Serialize(new
            {
                memberUserId = domainEvent.MemberUserId,
                oldRole = domainEvent.OldRole.ToString(),
                newRole = domainEvent.NewRole.ToString()
            });

            return activityLogWriter.WriteTeamActivityAsync(domainEvent.TeamId, domainEvent.ActorUserId,
                ActivityTypes.TeamMemberRoleChanged, EntityType.TeamMember, domainEvent.MemberId.ToString(), metadata, cancellationToken);
        }
    }
}