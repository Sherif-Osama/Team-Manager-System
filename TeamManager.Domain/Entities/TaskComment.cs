using TeamManager.Domain.Common;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class TaskComment : Entity<long>
{
    private readonly List<CommentMention> _mentions = new();

    public long TaskId { get; private set; }
    public TaskItem Task { get; private set; } = null!;
    public Guid AuthorUserId { get; private set; }
    public User Author { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public IReadOnlyCollection<CommentMention> Mentions => _mentions.AsReadOnly();

    private TaskComment()
    {
    }

    internal TaskComment(long taskId, Guid authorUserId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("A comment cannot be empty.");

        TaskId = taskId;
        AuthorUserId = authorUserId;
        Content = content;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Edit(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("A comment cannot be empty.");

        Content = content;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SoftDelete() => DeletedAtUtc = DateTime.UtcNow;

    public void Mention(Guid mentionedUserId)
    {
        if (_mentions.Any(m => m.MentionedUserId == mentionedUserId))
            return;

        _mentions.Add(new CommentMention(Id, mentionedUserId));
    }
}
