using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events.Comments
{
    public sealed record CommentMentionedDomainEvent(TaskComment Comment, Guid AuthorUserId, Guid MentionedUserId)
        : IDomainEvent;
}