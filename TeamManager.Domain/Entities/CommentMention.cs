using TeamManager.Domain.Common;

namespace TeamManager.Domain.Entities;

public class CommentMention : Entity<long>
{
    public long TaskCommentId { get; private set; }
    public TaskComment TaskComment { get; private set; } = null!;
    public Guid MentionedUserId { get; private set; }
    public User MentionedUser { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    private CommentMention()
    {
    }

    internal CommentMention(long taskCommentId, Guid mentionedUserId)
    {
        TaskCommentId = taskCommentId;
        MentionedUserId = mentionedUserId;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
