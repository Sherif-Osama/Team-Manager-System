using TeamManager.Domain.Common;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class TaskChecklistItem : Entity<long>
{
    public long TaskId { get; private set; }
    public TaskItem Task { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public bool IsCompleted { get; private set; }
    public short SortOrder { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public User? CompletedByUser { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private TaskChecklistItem() { }

    internal TaskChecklistItem(long taskId, string content, short sortOrder)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("A checklist item must have content.");

        TaskId = taskId;
        Content = content;
        SortOrder = sortOrder;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Complete(Guid completedBy)
    {
        IsCompleted = true;
        CompletedAtUtc = DateTime.UtcNow;
        CompletedBy = completedBy;
    }

    public void Reopen()
    {
        IsCompleted = false;
        CompletedAtUtc = null;
        CompletedBy = null;
    }
}
