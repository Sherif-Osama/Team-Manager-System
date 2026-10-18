using System.Reflection;
using TeamManager.Domain.Common;
using TeamManager.Domain.Common.Events.Comments;
using TeamManager.Domain.Common.Events.Tasks;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.UnitTest.Domain
{
    /// <summary>
    /// Tests TaskItem aggregate behavior: lifecycle, status transitions,
    /// scheduling, and child collections (dependencies, labels, checklist,
    /// attachments, comments).
    /// </summary>
    public sealed class TaskItemTests
    {
        private static readonly Guid ProjectId = Guid.NewGuid();
        private static readonly Guid Creator = Guid.NewGuid();

        #region Helper Methods
        private static TaskItem CreateTask(string title = "Test Task",
            TaskPriority priority = TaskPriority.Medium,
            Guid? assignee = null,
            DateOnly? startDate = null, DateOnly? dueDate = null,
            DateOnly? projectStartDate = null, DateOnly? projectDueDate = null)
        {
            return new TaskItem(ProjectId, title, Creator, priority,
                userId: assignee, startDate: startDate, dueDate: dueDate,
                projectStartDate: projectStartDate, projectDueDate: projectDueDate);
        }

        private static TaskItem CreateTaskInStatus(TaskItemStatus status)
        {
            var task = CreateTask();
            var actor = task.CreatedBy;

            switch (status)
            {
                case TaskItemStatus.Todo:
                    break;
                case TaskItemStatus.InProgress:
                    task.ChangeStatus(TaskItemStatus.InProgress, actor);
                    break;
                case TaskItemStatus.InReview:
                    task.ChangeStatus(TaskItemStatus.InProgress, actor);
                    task.ChangeStatus(TaskItemStatus.InReview, actor);
                    break;
                case TaskItemStatus.Done:
                    task.ChangeStatus(TaskItemStatus.InProgress, actor);
                    task.ChangeStatus(TaskItemStatus.InReview, actor);
                    task.ChangeStatus(TaskItemStatus.Done, actor);
                    break;
                case TaskItemStatus.Cancelled:
                    task.ChangeStatus(TaskItemStatus.Cancelled, actor);
                    break;
            }

            return task;
        }

        private static void SetId(Entity<long> entity, long id)
        {
            var property = typeof(Entity<long>).GetProperty(nameof(Entity<long>.Id),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;

            property.SetValue(entity, id);
        }
        #endregion

        #region Constructor
        [Fact]
        public void Constructor_WithValidData_CreatesTodoTask()
        {
            var before = DateTime.UtcNow;

            var task = CreateTask(priority: TaskPriority.High);

            Assert.Equal(ProjectId, task.ProjectId);
            Assert.Equal("Test Task", task.Title);
            Assert.Equal(Creator, task.CreatedBy);
            Assert.Equal(TaskItemStatus.Todo, task.Status);
            Assert.Equal(TaskPriority.High, task.Priority);
            Assert.Null(task.AssigneeUserId);
            Assert.Null(task.StartDate);
            Assert.Null(task.DueDate);
            Assert.Null(task.CompletedAtUtc);
            Assert.InRange(task.CreatedAtUtc, before, DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_WithAssignee_AssignsAndRaisesAssignedEvent()
        {
            var assignee = Guid.NewGuid();

            var task = CreateTask(assignee: assignee);

            Assert.Equal(assignee, task.AssigneeUserId);
            Assert.Contains(task.DomainEvents, e => e is TaskAssignedDomainEvent { AssignedUserId: var u } && u == assignee);
        }

        [Fact]
        public void Constructor_WithValidDates_SetsDates()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = today.AddDays(1);
            var due = today.AddDays(5);

            var task = CreateTask(startDate: start, dueDate: due);

            Assert.Equal(start, task.StartDate);
            Assert.Equal(due, task.DueDate);
        }

        [Fact]
        public void Constructor_WithPastStartDate_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            Assert.Throws<DomainException>(() => CreateTask(startDate: today.AddDays(-1)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankTitle_Throws(string? title)
        {
            Assert.Throws<DomainException>(() => CreateTask(title: title!));
        }
        #endregion

        #region Rename
        [Fact]
        public void Rename_WithValidTitle_UpdatesTitleAndTimestamp()
        {
            var task = CreateTask();
            var before = DateTime.UtcNow;

            task.Rename("Renamed Task");

            Assert.Equal("Renamed Task", task.Title);
            Assert.NotNull(task.UpdatedAtUtc);
            Assert.InRange(task.UpdatedAtUtc!.Value, before, DateTime.UtcNow);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Rename_WithBlankTitle_Throws(string? title)
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.Rename(title!));
        }

        [Fact]
        public void Rename_WhenDeleted_Throws()
        {
            var task = CreateTask();
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.Rename("New Title"));
        }
        #endregion

        #region Update Description
        [Fact]
        public void UpdateDescription_WithValue_UpdatesAndTouches()
        {
            var task = CreateTask();

            task.UpdateDescription("New description");

            Assert.Equal("New description", task.Description);
            Assert.NotNull(task.UpdatedAtUtc);
        }

        [Fact]
        public void UpdateDescription_WithNull_ClearsDescription()
        {
            var task = CreateTask();
            task.UpdateDescription("Initial");

            task.UpdateDescription(null);

            Assert.Null(task.Description);
        }

        [Fact]
        public void UpdateDescription_WhenDeleted_Throws()
        {
            var task = CreateTask();
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.UpdateDescription("New"));
        }
        #endregion

        #region Assign
        [Fact]
        public void Assign_WhenUnassigned_AssignsAndRaisesEvent()
        {
            var task = CreateTask();
            var userId = Guid.NewGuid();

            task.Assign(userId, Creator);

            Assert.Equal(userId, task.AssigneeUserId);
            Assert.Contains(task.DomainEvents, e => e is TaskAssignedDomainEvent
            {
                PreviousAssigneeUserId: null, AssignedUserId: var assigned, ActorUserId: var actor
            } && assigned == userId && actor == Creator);
        }

        [Fact]
        public void Assign_ToDifferentUser_KeepsPreviousAssigneeInEvent()
        {
            var firstAssignee = Guid.NewGuid();
            var secondAssignee = Guid.NewGuid();
            var task = CreateTask(assignee: firstAssignee);

            task.Assign(secondAssignee, Creator);

            Assert.Equal(secondAssignee, task.AssigneeUserId);
            Assert.Contains(task.DomainEvents, e => e is TaskAssignedDomainEvent
            {
                PreviousAssigneeUserId: var previous, AssignedUserId: var assigned
            } && previous == firstAssignee && assigned == secondAssignee);
        }

        [Fact]
        public void Assign_WithSameAssignee_Throws()
        {
            var assignee = Guid.NewGuid();
            var task = CreateTask(assignee: assignee);

            Assert.Throws<DomainException>(() => task.Assign(assignee, Creator));
        }

        [Fact]
        public void Assign_WhenCancelled_Throws()
        {
            var task = CreateTaskInStatus(TaskItemStatus.Cancelled);

            Assert.Throws<DomainException>(() => task.Assign(Guid.NewGuid(), Creator));
        }

        [Fact]
        public void Assign_WhenDeleted_Throws()
        {
            var task = CreateTask();
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.Assign(Guid.NewGuid(), Creator));
        }
        #endregion

        #region Unassign
        [Fact]
        public void Unassign_WhenAssigned_ClearsAssignee()
        {
            var task = CreateTask(assignee: Guid.NewGuid());

            task.Unassign();

            Assert.Null(task.AssigneeUserId);
            Assert.NotNull(task.UpdatedAtUtc);
        }

        [Fact]
        public void Unassign_WhenNotAssigned_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.Unassign());
        }

        [Fact]
        public void Unassign_WhenDeleted_Throws()
        {
            var task = CreateTask(assignee: Guid.NewGuid());
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.Unassign());
        }
        #endregion

        #region Change Priority
        [Fact]
        public void ChangePriority_WithDifferentPriority_UpdatesAndTouches()
        {
            var task = CreateTask(priority: TaskPriority.Low);

            task.ChangePriority(TaskPriority.Urgent);

            Assert.Equal(TaskPriority.Urgent, task.Priority);
            Assert.NotNull(task.UpdatedAtUtc);
        }

        [Fact]
        public void ChangePriority_WithSamePriority_Throws()
        {
            var task = CreateTask(priority: TaskPriority.Medium);

            Assert.Throws<DomainException>(() => task.ChangePriority(TaskPriority.Medium));
        }

        [Fact]
        public void ChangePriority_WhenDone_Throws()
        {
            var task = CreateTaskInStatus(TaskItemStatus.Done);

            Assert.Throws<DomainException>(() => task.ChangePriority(TaskPriority.High));
        }

        [Fact]
        public void ChangePriority_WhenCancelled_Throws()
        {
            var task = CreateTaskInStatus(TaskItemStatus.Cancelled);

            Assert.Throws<DomainException>(() => task.ChangePriority(TaskPriority.High));
        }

        [Fact]
        public void ChangePriority_WhenDeleted_Throws()
        {
            var task = CreateTask();
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.ChangePriority(TaskPriority.High));
        }
        #endregion

        #region Change Status
        [Theory]
        [InlineData(TaskItemStatus.Todo, TaskItemStatus.InProgress)]
        [InlineData(TaskItemStatus.Todo, TaskItemStatus.Cancelled)]
        [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Todo)]
        [InlineData(TaskItemStatus.InProgress, TaskItemStatus.InReview)]
        [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Cancelled)]
        [InlineData(TaskItemStatus.InReview, TaskItemStatus.InProgress)]
        [InlineData(TaskItemStatus.InReview, TaskItemStatus.Done)]
        [InlineData(TaskItemStatus.InReview, TaskItemStatus.Cancelled)]
        [InlineData(TaskItemStatus.Done, TaskItemStatus.InProgress)]
        [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.Todo)]
        public void ChangeStatus_WithValidTransition_ChangesStatusAndRaisesEvent(TaskItemStatus from, TaskItemStatus to)
        {
            var task = CreateTaskInStatus(from);
            var before = DateTime.UtcNow;

            task.ChangeStatus(to, Creator);

            Assert.Equal(to, task.Status);
            Assert.Equal(to == TaskItemStatus.Done, task.CompletedAtUtc.HasValue);

            if (to == TaskItemStatus.Done)
                Assert.InRange(task.CompletedAtUtc!.Value, before, DateTime.UtcNow);

            Assert.Contains(task.DomainEvents, e => e is TaskStatusChangedDomainEvent
            {
                FromStatus: var fromRaised, ToStatus: var toRaised
            } && fromRaised == from && toRaised == to);

            Assert.NotNull(task.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(TaskItemStatus.Todo, TaskItemStatus.Done)]
        [InlineData(TaskItemStatus.Todo, TaskItemStatus.InReview)]
        [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Done)]
        [InlineData(TaskItemStatus.InReview, TaskItemStatus.Todo)]
        [InlineData(TaskItemStatus.Done, TaskItemStatus.Todo)]
        [InlineData(TaskItemStatus.Done, TaskItemStatus.Cancelled)]
        [InlineData(TaskItemStatus.Done, TaskItemStatus.InReview)]
        [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.InProgress)]
        [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.Done)]
        [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.InReview)]
        public void ChangeStatus_WithInvalidTransition_ThrowsAndKeepsStatus(TaskItemStatus from, TaskItemStatus to)
        {
            var task = CreateTaskInStatus(from);

            Assert.Throws<DomainException>(() => task.ChangeStatus(to, Creator));
            Assert.Equal(from, task.Status);
        }

        [Fact]
        public void ChangeStatus_WithSameStatus_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.ChangeStatus(TaskItemStatus.Todo, Creator));
        }

        [Fact]
        public void ChangeStatus_ToDone_WithIncompleteChecklist_Throws()
        {
            var task = CreateTaskInStatus(TaskItemStatus.InReview);
            task.AddChecklistItem("First item");

            Assert.Throws<DomainException>(() => task.ChangeStatus(TaskItemStatus.Done, Creator));
            Assert.Equal(TaskItemStatus.InReview, task.Status);
        }

        [Fact]
        public void ChangeStatus_ToDone_WhenChecklistComplete_Succeeds()
        {
            var task = CreateTaskInStatus(TaskItemStatus.InReview);
            var item = task.AddChecklistItem("First item");

            task.CompleteChecklistItem(item.Id, Creator);
            task.ChangeStatus(TaskItemStatus.Done, Creator);

            Assert.Equal(TaskItemStatus.Done, task.Status);
            Assert.NotNull(task.CompletedAtUtc);
        }

        [Fact]
        public void ChangeStatus_LeavingDone_ClearsCompletedAt()
        {
            var task = CreateTaskInStatus(TaskItemStatus.Done);

            task.ChangeStatus(TaskItemStatus.InProgress, Creator);

            Assert.Null(task.CompletedAtUtc);
        }

        [Fact]
        public void ChangeStatus_WhenDeleted_Throws()
        {
            var task = CreateTask();
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.ChangeStatus(TaskItemStatus.InProgress, Creator));
        }
        #endregion

        #region Reschedule
        [Fact]
        public void Reschedule_WithValidDates_UpdatesDatesAndTouches()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var task = CreateTask();

            task.Reschedule(today.AddDays(1), today.AddDays(5));

            Assert.Equal(today.AddDays(1), task.StartDate);
            Assert.Equal(today.AddDays(5), task.DueDate);
            Assert.NotNull(task.UpdatedAtUtc);
        }

        [Fact]
        public void Reschedule_WithOnlyDueDate_KeepsExistingStartDate()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = today.AddDays(1);
            var task = CreateTask(startDate: start);

            task.Reschedule(null, today.AddDays(5));

            Assert.Equal(start, task.StartDate);
            Assert.Equal(today.AddDays(5), task.DueDate);
        }

        [Fact]
        public void Reschedule_WithDueBeforeStart_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.Reschedule(today.AddDays(5), today.AddDays(1)));
        }

        [Fact]
        public void Reschedule_WithPastStartDate_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.Reschedule(today.AddDays(-1), today.AddDays(5)));
        }

        [Fact]
        public void Reschedule_WithPastDueDate_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.Reschedule(today.AddDays(1), today.AddDays(-1)));
        }

        [Fact]
        public void Reschedule_BeforeProjectStartDate_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.Reschedule(today.AddDays(1), today.AddDays(5),
                projectStartDate: today.AddDays(10)));
        }

        [Fact]
        public void Reschedule_AfterProjectDueDate_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.Reschedule(today.AddDays(1), today.AddDays(5),
                projectDueDate: today.AddDays(2)));
        }

        [Fact]
        public void Reschedule_WhenCancelled_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var task = CreateTaskInStatus(TaskItemStatus.Cancelled);

            Assert.Throws<DomainException>(() => task.Reschedule(today.AddDays(1), today.AddDays(5)));
        }

        [Fact]
        public void Reschedule_WhenDeleted_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var task = CreateTask();
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.Reschedule(today.AddDays(1), today.AddDays(5)));
        }
        #endregion

        #region Soft Delete
        [Fact]
        public void SoftDelete_WhenNotDeleted_SetsCancelledAndDeleted()
        {
            var task = CreateTask();
            var before = DateTime.UtcNow;

            task.SoftDelete();

            Assert.Equal(TaskItemStatus.Cancelled, task.Status);
            Assert.NotNull(task.DeletedAtUtc);
            Assert.InRange(task.DeletedAtUtc!.Value, before, DateTime.UtcNow);
            Assert.Null(task.CompletedAtUtc);
        }

        [Fact]
        public void SoftDelete_WhenDone_ClearsCompletedAt()
        {
            var task = CreateTaskInStatus(TaskItemStatus.Done);

            task.SoftDelete();

            Assert.Null(task.CompletedAtUtc);
        }

        [Fact]
        public void SoftDelete_WhenAlreadyDeleted_Throws()
        {
            var task = CreateTask();
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.SoftDelete());
        }
        #endregion

        #region Dependencies
        [Fact]
        public void AddDependency_WithOtherTask_CreatesDependencyAndTouches()
        {
            var task = CreateTask();
            var createdBy = Guid.NewGuid();

            var dependency = task.AddDependency(42, createdBy);

            Assert.Equal(task.Id, dependency.TaskId);
            Assert.Equal(42, dependency.DependsOnTaskId);
            Assert.Equal(createdBy, dependency.CreatedBy);
            Assert.Single(task.Dependencies);
            Assert.NotNull(task.UpdatedAtUtc);
        }

        [Fact]
        public void AddDependency_OnSelf_Throws()
        {
            var task = CreateTask();
            SetId(task, 7);

            Assert.Throws<DomainException>(() => task.AddDependency(7, Creator));
        }

        [Fact]
        public void AddDependency_Duplicate_Throws()
        {
            var task = CreateTask();
            task.AddDependency(42, Creator);

            Assert.Throws<DomainException>(() => task.AddDependency(42, Creator));
        }

        [Fact]
        public void AddDependency_WhenCancelled_Throws()
        {
            var task = CreateTaskInStatus(TaskItemStatus.Cancelled);

            Assert.Throws<DomainException>(() => task.AddDependency(42, Creator));
        }

        [Fact]
        public void AddDependency_WhenDeleted_Throws()
        {
            var task = CreateTask();
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.AddDependency(42, Creator));
        }

        [Fact]
        public void RemoveDependency_WhenExists_RemovesIt()
        {
            var task = CreateTask();
            var dependency = task.AddDependency(42, Creator);

            var removed = task.RemoveDependency(dependency.Id);

            Assert.Same(dependency, removed);
            Assert.Empty(task.Dependencies);
        }

        [Fact]
        public void RemoveDependency_WhenNotFound_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.RemoveDependency(999));
        }
        #endregion

        #region Labels
        [Fact]
        public void AddLabel_WhenNotApplied_AddsLabelAndTouches()
        {
            var task = CreateTask();

            task.AddLabel(5);

            Assert.Single(task.Labels, l => l.LabelId == 5);
            Assert.NotNull(task.UpdatedAtUtc);
        }

        [Fact]
        public void AddLabel_Duplicate_Throws()
        {
            var task = CreateTask();
            task.AddLabel(5);

            Assert.Throws<DomainException>(() => task.AddLabel(5));
        }

        [Fact]
        public void AddLabel_WhenCancelled_Throws()
        {
            var task = CreateTaskInStatus(TaskItemStatus.Cancelled);

            Assert.Throws<DomainException>(() => task.AddLabel(5));
        }

        [Fact]
        public void RemoveLabel_WhenApplied_RemovesIt()
        {
            var task = CreateTask();
            task.AddLabel(5);

            task.RemoveLabel(5);

            Assert.Empty(task.Labels);
        }

        [Fact]
        public void RemoveLabel_WhenNotApplied_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.RemoveLabel(5));
        }
        #endregion

        #region Checklist
        [Fact]
        public void AddChecklistItem_AssignsSequentialSortOrder()
        {
            var task = CreateTask();

            var first = task.AddChecklistItem("First");
            var second = task.AddChecklistItem("Second");
            var third = task.AddChecklistItem("Third");

            Assert.Equal(0, first.SortOrder);
            Assert.Equal(1, second.SortOrder);
            Assert.Equal(2, third.SortOrder);
        }

        [Fact]
        public void AddChecklistItem_WhenCancelled_Throws()
        {
            var task = CreateTaskInStatus(TaskItemStatus.Cancelled);

            Assert.Throws<DomainException>(() => task.AddChecklistItem("Item"));
        }

        [Fact]
        public void AddChecklistItem_WhenDeleted_Throws()
        {
            var task = CreateTask();
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.AddChecklistItem("Item"));
        }

        [Fact]
        public void CompleteChecklistItem_WhenIncomplete_CompletesIt()
        {
            var task = CreateTask();
            var item = task.AddChecklistItem("Item");
            var completedBy = Guid.NewGuid();
            var before = DateTime.UtcNow;

            task.CompleteChecklistItem(item.Id, completedBy);

            Assert.True(item.IsCompleted);
            Assert.Equal(completedBy, item.CompletedBy);
            Assert.NotNull(item.CompletedAtUtc);
            Assert.InRange(item.CompletedAtUtc!.Value, before, DateTime.UtcNow);
        }

        [Fact]
        public void CompleteChecklistItem_WhenAlreadyCompleted_Throws()
        {
            var task = CreateTask();
            var item = task.AddChecklistItem("Item");
            task.CompleteChecklistItem(item.Id, Creator);

            Assert.Throws<DomainException>(() => task.CompleteChecklistItem(item.Id, Creator));
        }

        [Fact]
        public void CompleteChecklistItem_WhenNotFound_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.CompleteChecklistItem(999, Creator));
        }

        [Fact]
        public void ReopenChecklistItem_WhenCompleted_ReopensIt()
        {
            var task = CreateTask();
            var item = task.AddChecklistItem("Item");
            task.CompleteChecklistItem(item.Id, Creator);

            task.ReopenChecklistItem(item.Id);

            Assert.False(item.IsCompleted);
            Assert.Null(item.CompletedAtUtc);
            Assert.Null(item.CompletedBy);
        }

        [Fact]
        public void ReopenChecklistItem_WhenIncomplete_Throws()
        {
            var task = CreateTask();
            var item = task.AddChecklistItem("Item");

            Assert.Throws<DomainException>(() => task.ReopenChecklistItem(item.Id));
        }

        [Fact]
        public void RemoveChecklistItem_WhenExists_RemovesIt()
        {
            var task = CreateTask();
            var item = task.AddChecklistItem("Item");

            task.RemoveChecklistItem(item.Id);

            Assert.Empty(task.ChecklistItems);
        }

        [Fact]
        public void RemoveChecklistItem_WhenNotFound_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.RemoveChecklistItem(999));
        }
        #endregion

        #region Attachments
        [Fact]
        public void AddAttachment_WithValidData_AddsAttachment()
        {
            var task = CreateTask();
            var uploadedBy = Guid.NewGuid();

            var attachment = task.AddAttachment("file.pdf", "storage-key", "application/pdf", 1024,
                uploadedBy, "hash");

            Assert.Equal(task.Id, attachment.TaskId);
            Assert.Equal("file.pdf", attachment.OriginalFileName);
            Assert.Equal("storage-key", attachment.StorageKey);
            Assert.Equal(1024, attachment.SizeBytes);
            Assert.Equal(uploadedBy, attachment.UploadedBy);
            Assert.Equal("hash", attachment.FileHash);
            Assert.Single(task.Attachments);
        }

        [Fact]
        public void AddAttachment_WithBlankFileName_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.AddAttachment("", "storage-key", "application/pdf", 1024,
                Guid.NewGuid(), "hash"));
        }

        [Fact]
        public void AddAttachment_WithNonPositiveSize_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.AddAttachment("file.pdf", "storage-key", "application/pdf", 0,
                Guid.NewGuid(), "hash"));
        }

        [Fact]
        public void AddAttachment_WhenCancelled_Throws()
        {
            var task = CreateTaskInStatus(TaskItemStatus.Cancelled);

            Assert.Throws<DomainException>(() => task.AddAttachment("file.pdf", "storage-key", "application/pdf", 1024,
                Guid.NewGuid(), "hash"));
        }

        [Fact]
        public void AddAttachment_WhenDeleted_Throws()
        {
            var task = CreateTask();
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.AddAttachment("file.pdf", "storage-key", "application/pdf", 1024,
                Guid.NewGuid(), "hash"));
        }

        [Fact]
        public void RemoveAttachment_WhenExists_RemovesIt()
        {
            var task = CreateTask();
            var attachment = task.AddAttachment("file.pdf", "storage-key", "application/pdf", 1024,
                Guid.NewGuid(), "hash");

            var removed = task.RemoveAttachment(attachment.Id);

            Assert.Same(attachment, removed);
            Assert.Empty(task.Attachments);
        }

        [Fact]
        public void RemoveAttachment_WhenNotFound_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.RemoveAttachment(999));
        }
        #endregion

        #region Comments
        [Fact]
        public void AddComment_WithValidContent_AddsCommentAndRaisesEvent()
        {
            var task = CreateTask();
            var author = Guid.NewGuid();

            var comment = task.AddComment(author, "Nice work");

            Assert.Equal(task.Id, comment.TaskId);
            Assert.Equal(author, comment.AuthorUserId);
            Assert.Equal("Nice work", comment.Content);
            Assert.Contains(task.DomainEvents, e => e is CommentAddedDomainEvent { AuthorUserId: var a } && a == author);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddComment_WithBlankContent_Throws(string? content)
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.AddComment(Guid.NewGuid(), content!));
        }

        [Fact]
        public void AddComment_WithMentions_RaisesEventPerMentionedUser()
        {
            var task = CreateTask();
            var author = Guid.NewGuid();
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();

            task.AddComment(author, "Hello", [first, second, author]);

            Assert.Equal(2, task.DomainEvents.Count(e => e is CommentMentionedDomainEvent));
            Assert.Contains(task.DomainEvents, e => e is CommentMentionedDomainEvent { MentionedUserId: var u } && u == first);
            Assert.Contains(task.DomainEvents, e => e is CommentMentionedDomainEvent { MentionedUserId: var u } && u == second);
        }

        [Fact]
        public void AddComment_WithDuplicateMentions_RaisesEventOnce()
        {
            var task = CreateTask();
            var author = Guid.NewGuid();
            var mentioned = Guid.NewGuid();

            task.AddComment(author, "Hello", [mentioned, mentioned]);

            Assert.Single(task.DomainEvents, e => e is CommentMentionedDomainEvent);
        }

        [Fact]
        public void AddComment_WithEmptyMentionedUserId_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.AddComment(Guid.NewGuid(), "Hello", [Guid.Empty]));
        }

        [Fact]
        public void AddComment_WhenCancelled_Throws()
        {
            var task = CreateTaskInStatus(TaskItemStatus.Cancelled);

            Assert.Throws<DomainException>(() => task.AddComment(Guid.NewGuid(), "Hello"));
        }

        [Fact]
        public void AddComment_WhenDeleted_Throws()
        {
            var task = CreateTask();
            task.SoftDelete();

            Assert.Throws<DomainException>(() => task.AddComment(Guid.NewGuid(), "Hello"));
        }

        [Fact]
        public void EditComment_WithValidContent_UpdatesContentAndTimestamp()
        {
            var task = CreateTask();
            var comment = task.AddComment(Guid.NewGuid(), "Original");
            var before = DateTime.UtcNow;

            task.EditComment(comment.Id, "Updated");

            Assert.Equal("Updated", comment.Content);
            Assert.NotNull(comment.UpdatedAtUtc);
            Assert.InRange(comment.UpdatedAtUtc!.Value, before, DateTime.UtcNow);
        }

        [Fact]
        public void EditComment_WithBlankContent_Throws()
        {
            var task = CreateTask();
            var comment = task.AddComment(Guid.NewGuid(), "Original");

            Assert.Throws<DomainException>(() => task.EditComment(comment.Id, "   "));
            Assert.Equal("Original", comment.Content);
        }

        [Fact]
        public void EditComment_WhenNotFound_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.EditComment(999, "Updated"));
        }

        [Fact]
        public void EditComment_AddingMentions_RaisesEventForNewMentionsOnly()
        {
            var task = CreateTask();
            var author = Guid.NewGuid();
            var existing = Guid.NewGuid();
            var added = Guid.NewGuid();
            var comment = task.AddComment(author, "Hello", [existing]);

            task.EditComment(comment.Id, "Hello again", [existing, added]);

            Assert.Equal(2, task.DomainEvents.Count(e => e is CommentMentionedDomainEvent));
            Assert.Contains(task.DomainEvents, e => e is CommentMentionedDomainEvent { MentionedUserId: var u } && u == added);
        }

        [Fact]
        public void DeleteComment_WhenExists_RemovesIt()
        {
            var task = CreateTask();
            var comment = task.AddComment(Guid.NewGuid(), "Hello");

            task.DeleteComment(comment.Id);

            Assert.Empty(task.Comments);
        }

        [Fact]
        public void DeleteComment_WhenNotFound_Throws()
        {
            var task = CreateTask();

            Assert.Throws<DomainException>(() => task.DeleteComment(999));
        }
        #endregion

        #region Domain Events
        [Fact]
        public void Constructor_RaisesTaskCreatedDomainEvent()
        {
            var task = CreateTask();

            Assert.Contains(task.DomainEvents, e => e is TaskCreatedDomainEvent);
        }
        #endregion
    }
}
