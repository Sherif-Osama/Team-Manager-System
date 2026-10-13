using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.AuditLog;
using TeamManager.Application.Common.AuditLog;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateAuditLogOnUserReactivatedHandler(IAuditLogWriter auditLogWriter)
        : INotificationHandler<DomainEventNotification<UserReactivatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserReactivatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var details = JsonSerializer.Serialize(new
            {
                reason = "Reactivate User"
            });

            return auditLogWriter.WriteAsync(AuditLogActions.UserReactivated, EntityType.User, domainEvent.UserId.ToString(),
                details, cancellationToken);
        }
    }
}