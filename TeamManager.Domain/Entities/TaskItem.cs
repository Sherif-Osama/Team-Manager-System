using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;
using TaskStatus = TeamManager.Domain.Enums.TaskStatus;

namespace TeamManager.Domain.Entities;

public class TaskItem : Entity<long>
{
    private readonly List<TaskDependency> _dependencies = new();
    private readonly List<TaskLabel> _labels = new();
    private readonly List<TaskChecklistItem> _checklistItems = new();
    private readonly List<TaskAttachment> _attachments = new();
    private readonly List<TaskComment> _comments = new();

    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public Guid CreatedBy { get; private set; }
    public User Creator { get; private set; } = null!;
    public Guid? AssigneeUserId { get; private set; }
    public User? Assignee { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public IReadOnlyCollection<TaskDependency> Dependencies => _dependencies.AsReadOnly();
    public IReadOnlyCollection<TaskLabel> Labels => _labels.AsReadOnly();
    public IReadOnlyCollection<TaskChecklistItem> ChecklistItems => _checklistItems.AsReadOnly();
    public IReadOnlyCollection<TaskAttachment> Attachments => _attachments.AsReadOnly();
    public IReadOnlyCollection<TaskComment> Comments => _comments.AsReadOnly();

    private TaskItem()
    {
    }

    public TaskItem(Guid projectId, string title, Guid createdBy, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("A task must have a title.");

        ProjectId = projectId;
        Title = title;
        Description = description;
        CreatedBy = createdBy;
        Status = TaskStatus.Todo;
        Priority = TaskPriority.Medium;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("A task must have a title.");

        Title = title;
        Touch();
    }

    public void Assign(Guid userId)
    {
        AssigneeUserId = userId;
        Touch();
    }

    public void Unassign()
    {
        AssigneeUserId = null;
        Touch();
    }

    public void ChangePriority(TaskPriority priority)
    {
        Priority = priority;
        Touch();
    }

    public void ChangeStatus(TaskStatus status)
    {
        Status = status;
        CompletedAtUtc = status == TaskStatus.Done ? DateTime.UtcNow : null;
        Touch();
    }

    public void Reschedule(DateOnly? startDate, DateOnly? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && dueDate.Value < startDate.Value)
            throw new DomainException("A task's due date cannot be before its start date.");

        StartDate = startDate;
        DueDate = dueDate;
        Touch();
    }

    public void SoftDelete() => DeletedAtUtc = DateTime.UtcNow;

    public TaskDependency AddDependency(long dependsOnTaskId, Guid createdBy)
    {
        if (dependsOnTaskId == Id)
            throw new DomainException("A task cannot depend on itself.");
        if (_dependencies.Any(d => d.DependsOnTaskId == dependsOnTaskId))
            throw new DomainException("This dependency already exists.");

        var dependency = new TaskDependency(Id, dependsOnTaskId, createdBy);
        _dependencies.Add(dependency);
        return dependency;
    }

    public TaskLabel AddLabel(long labelId)
    {
        if (_labels.Any(l => l.LabelId == labelId))
            throw new DomainException("This label is already applied to the task.");

        var taskLabel = new TaskLabel(Id, labelId);
        _labels.Add(taskLabel);
        return taskLabel;
    }

    public void RemoveLabel(long labelId)
    {
        var taskLabel = _labels.FirstOrDefault(l => l.LabelId == labelId);
        if (taskLabel is not null)
            _labels.Remove(taskLabel);
    }

    public TaskChecklistItem AddChecklistItem(string content)
    {
        short nextOrder = (short)(_checklistItems.Count == 0 ? 0 : _checklistItems.Max(c => c.SortOrder) + 1);
        var item = new TaskChecklistItem(Id, content, nextOrder);
        _checklistItems.Add(item);
        return item;
    }

    public TaskAttachment AddAttachment(string originalFileName, string storageKey, string contentType,
        long sizeBytes, Guid uploadedBy, string? fileHash = null)
    {
        var attachment = new TaskAttachment(Id, originalFileName, storageKey, contentType, sizeBytes,
            uploadedBy, fileHash);
        _attachments.Add(attachment);
        return attachment;
    }

    public TaskComment AddComment(Guid authorUserId, string content)
    {
        var comment = new TaskComment(Id, authorUserId, content);
        _comments.Add(comment);
        return comment;
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
