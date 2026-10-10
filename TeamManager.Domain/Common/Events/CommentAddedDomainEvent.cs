using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events
{
    public sealed record CommentAddedDomainEvent(TaskComment Comment, Guid? AssigneeUserId, Guid AuthorUserId) : IDomainEvent;
}
