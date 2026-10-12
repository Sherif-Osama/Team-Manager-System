using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.AuditLog;
using TeamManager.Application.Common.AuditLog;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateAuditLogOnUserRoleAssignedHandler(IAuditLogWriter auditLogWriter) : INotificationHandler<DomainEventNotification<UserRoleAssignedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserRoleAssignedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var details = JsonSerializer.Serialize(new
            {
                roleId = domainEvent.RoleId
            });

            return auditLogWriter.WriteAsync(AuditLogActions.UserRoleAssigned, AuditLogEntityTypes.User,
                domainEvent.UserId.ToString(), details, cancellationToken);
        }
    }
}