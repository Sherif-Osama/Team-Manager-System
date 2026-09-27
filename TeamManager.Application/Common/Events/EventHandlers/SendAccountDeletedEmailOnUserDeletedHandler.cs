using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Outbox;
using TeamManager.Domain.Common.Events;

namespace TeamManager.Application.Common.Events.EventHandlers
{
    public sealed class SendAccountDeletedEmailOnUserDeletedHandler(IOutbox outbox, ICurrentUser currentUser)
        : INotificationHandler<DomainEventNotification<UserDeletedDomainEvent>>
    {
        public Task Handle(DomainEventNotification<UserDeletedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Serialize(new
            {
                To = notification.DomainEvent.Email,
                DeletedAtUtc = DateTime.UtcNow,
                DeviceInfo = currentUser.DeviceInfo
            });

            return outbox.Add(OutboxMessageType.AccountDeletedEmail, payload);
        }
    }
}