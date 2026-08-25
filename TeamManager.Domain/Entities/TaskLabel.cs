using TeamManager.Domain.Common;

namespace TeamManager.Domain.Entities;

public class TaskLabel : Entity<(long TaskId, long LabelId)>
{
    public long TaskId { get; private set; }
    public TaskItem Task { get; private set; } = null!;
    public long LabelId { get; private set; }
    public Label Label { get; private set; } = null!;
    public DateTime AddedAtUtc { get; private set; }

    private TaskLabel() { }

    internal TaskLabel(long taskId, long labelId)
    {
        TaskId = taskId;
        LabelId = labelId;
        Id = (taskId, labelId);
        AddedAtUtc = DateTime.UtcNow;
    }
}
