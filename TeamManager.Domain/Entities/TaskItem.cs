using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;
namespace TeamManager.Domain.Entities
{

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
        public TaskItemStatus Status { get; private set; }
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

        private TaskItem() { }

        public TaskItem(Guid projectId, string title, Guid createdBy, TaskPriority priority, string? description = null,
        Guid? userId = null, DateOnly? startDate = null, DateOnly? dueDate = null,
             DateOnly? projectStartDate = null, DateOnly? projectDueDate = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("A task must have a title.");

            ProjectId = projectId;
            Title = title;
            AssigneeUserId = userId;
            Description = description;
            CreatedBy = createdBy;
            Status = TaskItemStatus.Todo;
            Priority = priority;
            CreatedAtUtc = DateTime.UtcNow;

            if (startDate.HasValue || dueDate.HasValue)
                Reschedule(startDate, dueDate, projectStartDate, projectDueDate);
        }

        public void Rename(string title)
        {
            EnsureNotDeleted("cannot modify deleted task");

            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("A task must have a title.");

            Title = title;
            Touch();
        }

        public void UpdateDescription(string? description)
        {
            EnsureNotDeleted("cannot modify deleted task");

            Description = description;
            Touch();
        }

        public void Assign(Guid userId)
        {
            EnsureNotDeleted("cannot assigned deleted task");

            EnsureNotCancelled("cannot assigned cancelled task");

            if (AssigneeUserId == userId)
                throw new DomainException("Task already assigned to this user");

            AssigneeUserId = userId;
            Touch();
        }

        public void Unassign()
        {
            EnsureNotDeleted("cannot modify deleted task");
            if (AssigneeUserId == null)
                throw new DomainException("The task is not currently assigned.");
            AssigneeUserId = null;
            Touch();
        }

        public void ChangePriority(TaskPriority priority)
        {
            EnsureNotDeleted("cannot modify deleted task");

            EnsureNotCancelled("cannot change priority cancelled task");

            if (Status == TaskItemStatus.Done)
                throw new DomainException("cannot change priority of a completed task.");

            if (Priority == priority)
                throw new DomainException($"Task already has priority {priority}.");
            Priority = priority;
            Touch();
        }

        public void ChangeStatus(TaskItemStatus status)
        {
            EnsureNotDeleted("cannot modify deleted task");

            if (Status == status)
                throw new DomainException($"Task already has status {status}.");

            if (!CanTransitionTo(status))
                throw new DomainException($"Task cannot transition from {Status} to {status}.");

            Status = status;

            CompletedAtUtc = status == TaskItemStatus.Done ? DateTime.UtcNow : null;

            Touch();
        }

        private bool CanTransitionTo(TaskItemStatus newStatus)
        {
            return Status switch
            {
                TaskItemStatus.Todo => newStatus is TaskItemStatus.InProgress or TaskItemStatus.Cancelled,

                TaskItemStatus.InProgress => newStatus is TaskItemStatus.Todo or TaskItemStatus.InReview or TaskItemStatus.Cancelled,

                TaskItemStatus.InReview => newStatus is TaskItemStatus.InProgress or TaskItemStatus.Done
                or TaskItemStatus.Cancelled,

                TaskItemStatus.Done => newStatus is TaskItemStatus.InProgress,

                TaskItemStatus.Cancelled => newStatus is TaskItemStatus.Todo,

                _ => false
            };
        }

        public void Reschedule(DateOnly? startDate, DateOnly? dueDate, DateOnly? projectStartDate = null,
            DateOnly? projectDueDate = null)
        {
            EnsureNotDeleted("cannot modify deleted task");
            EnsureNotCancelled("cannot modify cancelled task");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var effectiveStart = startDate ?? StartDate;
            var effectiveDue = dueDate ?? DueDate;

            if (effectiveStart.HasValue && effectiveDue.HasValue && effectiveDue.Value < effectiveStart.Value)
                throw new DomainException("A task's due date cannot be before its start date.");

            if (startDate.HasValue && startDate.Value < today)
                throw new DomainException("task start date cannot be in the past.");

            if (dueDate.HasValue && dueDate.Value < today)
                throw new DomainException("task due date cannot be in the past.");

            if (projectStartDate.HasValue && effectiveStart.HasValue && effectiveStart.Value < projectStartDate.Value)
                throw new DomainException("A task's start date cannot be before the project's start date.");

            if (projectDueDate.HasValue && effectiveDue.HasValue && effectiveDue.Value > projectDueDate.Value)
                throw new DomainException("A task's due date cannot be after the project's due date.");

            StartDate = effectiveStart;
            DueDate = effectiveDue;
            Touch();
        }

        public void SoftDelete()
        {
            EnsureNotDeleted("Task is already deleted");
            Status = TaskItemStatus.Cancelled;
            DeletedAtUtc = DateTime.UtcNow;
            CompletedAtUtc = null;
        }

        public TaskDependency AddDependency(long dependsOnTaskId, Guid createdBy)
        {
            EnsureNotDeleted("cannot add dependencies to deleted task");

            EnsureNotCancelled("cannot add dependencies to cancelled task");

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
            EnsureNotDeleted("cannot add label to deleted task");

            if (_labels.Any(l => l.LabelId == labelId))
                throw new DomainException("This label is already applied to the task.");

            var taskLabel = new TaskLabel(Id, labelId);
            _labels.Add(taskLabel);
            return taskLabel;
        }

        public void RemoveLabel(long labelId)
        {
            EnsureNotDeleted("cannot modify deleted task");

            var taskLabel = _labels.FirstOrDefault(l => l.LabelId == labelId);

            if (taskLabel is null)
                throw new DomainException("this label does not apply to the task");

            _labels.Remove(taskLabel);
        }

        public TaskChecklistItem AddChecklistItem(string content)
        {
            EnsureNotDeleted("cannot add check list to deleted task");

            EnsureNotCancelled("cannot add check list to cancelled task");

            short nextOrder = (short)(_checklistItems.Count == 0 ? 0 : _checklistItems.Max(c => c.SortOrder) + 1);
            var item = new TaskChecklistItem(Id, content, nextOrder);
            _checklistItems.Add(item);
            return item;
        }

        public TaskAttachment AddAttachment(string originalFileName, string storageKey, string contentType,
            long sizeBytes, Guid uploadedBy, string? fileHash = null)
        {
            EnsureNotDeleted("cannot add attchment to deleted task");

            EnsureNotCancelled("cannot add attchment to cancelled task");

            var attachment = new TaskAttachment(Id, originalFileName, storageKey, contentType, sizeBytes,
                uploadedBy, fileHash);
            _attachments.Add(attachment);
            return attachment;
        }

        public TaskComment AddComment(Guid authorUserId, string content)
        {
            EnsureNotDeleted("cannot add comment to deleted task");

            EnsureNotCancelled("cannot add comment to cancelled task");

            var comment = new TaskComment(Id, authorUserId, content);
            _comments.Add(comment);
            return comment;
        }


        private void EnsureNotDeleted(string message)
        {
            if (DeletedAtUtc.HasValue)
                throw new DomainException(message);
        }

        private void EnsureNotCancelled(string message)
        {
            if (Status == TaskItemStatus.Cancelled)
                throw new DomainException(message);
        }

        private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
    }
}