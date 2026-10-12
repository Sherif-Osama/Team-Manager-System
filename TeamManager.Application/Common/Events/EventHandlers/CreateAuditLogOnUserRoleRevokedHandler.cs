using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.AuditLog;
using TeamManager.Application.Common.AuditLog;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateAuditLogOnUserRoleRevokedHandler(IAuditLogWriter auditLogWriter) : INotificationHandler<DomainEventNotification<UserRoleRevokedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserRoleRevokedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var details = JsonSerializer.Serialize(new
            {
                roleId = domainEvent.RoleId
            });

            return auditLogWriter.WriteAsync(AuditLogActions.UserRoleRevoked, AuditLogEntityTypes.User, domainEvent.UserId.ToString(),
                details, cancellationToken);
        }
    }
}