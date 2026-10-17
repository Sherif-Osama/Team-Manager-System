namespace TeamManager.Domain.Common.Events.Comments
{
    public sealed record TaskCommentsChangedDomainEvent(long TaskId) : IDomainEvent;
}