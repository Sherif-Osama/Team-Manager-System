using MediatR;
using TeamManager.Domain.Common;

namespace TeamManager.Application.Common.Events
{
    public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification where TDomainEvent
        : IDomainEvent;
}