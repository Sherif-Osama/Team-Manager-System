using MediatR;
using TeamManager.Application.Abstractions.AuditLog;
using TeamManager.Application.Common.AuditLog;
using TeamManager.Application.Common.Logging;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{

    public sealed class CreateAuditLogOnUserDeactivatedHandler(IAuditLogWriter auditLogWriter)
        : INotificationHandler<DomainEventNotification<UserDeactivatedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserDeactivatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            return auditLogWriter.WriteAsync(AuditLogActions.UserDeactivated, LogEntityTypes.User, domainEvent.UserId.ToString(),
                null, cancellationToken);
        }
    }
}