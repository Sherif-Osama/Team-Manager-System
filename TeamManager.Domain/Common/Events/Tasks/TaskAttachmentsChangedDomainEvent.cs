namespace TeamManager.Domain.Common.Events.Tasks
{
    public sealed record TaskAttachmentsChangedDomainEvent(long TaskId) : IDomainEvent;
}