using MediatR;
using TeamManager.Application.Abstractions.AuditLog;
using TeamManager.Application.Common.AuditLog;
using TeamManager.Application.Common.EntityTypes;
using TeamManager.Domain.Common.Events.Users;

namespace TeamManager.Application.Common.Events.EventHandlers.AuditLog
{

    public sealed class CreateAuditLogOnUserDeactivatedHandler(IAuditLogWriter auditLogWriter)
        : INotificationHandler<DomainEventNotification<UserDeactivatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserDeactivatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            return auditLogWriter.WriteAsync(AuditLogActions.UserDeactivated, EntityType.User, domainEvent.UserId.ToString(),
                null, cancellationToken);
        }
    }
}