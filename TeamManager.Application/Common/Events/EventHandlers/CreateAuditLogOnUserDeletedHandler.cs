using MediatR;
using TeamManager.Application.Abstractions.AuditLog;
using TeamManager.Application.Common.AuditLog;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class CreateAuditLogOnUserDeletedHandler(IAuditLogWriter auditLogWriter)
        : INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            return auditLogWriter.WriteAsync(AuditLogActions.UserDeleted, AuditLogEntityTypes.User, domainEvent.UserId.ToString(),
                null, cancellationToken);
        }
    }
}