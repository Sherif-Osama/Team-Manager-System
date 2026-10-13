using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Common.ActivityLog;
using TeamManager.Application.Common.Logging;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateActivityLogOnProjectMemberRoleChangedHandler(IActivityLogWriter activityLogWriter)
        : INotificationHandler<DomainEventNotification<ProjectMemberRoleChangedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<ProjectMemberRoleChangedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var metadata = JsonSerializer.Serialize(new
            {
                memberUserId = domainEvent.MemberUserId,
                oldRole = domainEvent.OldRole.ToString(),
                newRole = domainEvent.NewRole.ToString()
            });

            return activityLogWriter.WriteProjectActivityAsync(domainEvent.ProjectId, domainEvent.ActorUserId,
                ActivityTypes.ProjectMemberRoleChanged, LogEntityTypes.ProjectMember, domainEvent.MemberId.ToString(), metadata, cancellationToken);
        }
    }
}