using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events
{
    public sealed record CommentAddedDomainEvent(TaskItem Task, TaskComment Comment, Guid? AssigneeUserId, Guid AuthorUserId) : IDomainEvent;
}