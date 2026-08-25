using TeamManager.Domain.Common;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class TaskDependency : Entity<long>
{
    public long TaskId { get; private set; }
    public TaskItem Task { get; private set; } = null!;
    public long DependsOnTaskId { get; private set; }
    public TaskItem DependsOnTask { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public User CreatedByUser { get; private set; } = null!;

    private TaskDependency() { }

    internal TaskDependency(long taskId, long dependsOnTaskId, Guid createdBy)
    {
        if (taskId == dependsOnTaskId)
            throw new DomainException("A task cannot depend on itself.");

        TaskId = taskId;
        DependsOnTaskId = dependsOnTaskId;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
