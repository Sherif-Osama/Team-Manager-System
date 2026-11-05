namespace TeamManager.Domain.Common.Events.Teams
{
    public record TeamDeletedDomainEvent(Guid teamId) : IDomainEvent;
}