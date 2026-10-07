using TeamManager.Domain.Entities;

namespace TeamManager.Domain.Common.Events
{
    public sealed record CommentMentionedDomainEvent(TaskComment Comment, Guid AuthorUserId, Guid MentionedUserId)
        : IDomainEvent;
}