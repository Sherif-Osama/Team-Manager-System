namespace TeamManager.Domain.Common.Events.Teams
{
    public sealed record TeamLabelsChangedDomainEvent(Guid TeamId) : IDomainEvent;
}