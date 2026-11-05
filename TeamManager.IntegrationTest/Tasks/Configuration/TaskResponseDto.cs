using TeamManager.Domain.Enums;

namespace TeamManager.IntegrationTest.Tasks.Configuration
{
    public sealed record GetTaskResponseDto(long Id, Guid ProjectId, string ProjectName, string Title, string? Description,
        TaskItemStatus Status, TaskPriority Priority, Guid CreatedBy, string CreatorName, Guid? AssigneeUserId, string? AssigneeName,
        DateOnly? StartDate, DateOnly? DueDate, DateTime? CompletedAtUtc, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc,
        int DependenciesCount, int LabelsCount, int ChecklistItemsCount, int AttachmentsCount, int CommentsCount);

    public sealed record TaskLabelResponseDto(long Id, string Name, string ColorHex);

    public sealed record GetTaskDependenciesResponseDto(IReadOnlyList<TaskDependencyItemDto> Items, int TotalCount,
        int Page, int PageSize);

    public sealed record TaskDependencyItemDto(long DependencyId, long TaskId, long DependsOnTaskId, string DependsOnTaskTitle,
        TaskItemStatus DependsOnTaskStatus, TaskPriority DependsOnTaskPriority, DateOnly? DependsOnTaskDueDate,
        Guid CreatedBy, string CreatedByName, DateTime CreatedAtUtc);

    public sealed record TaskChecklistItemDto(long Id, long TaskId, string Content, bool IsCompleted, short SortOrder,
        DateTime? CompletedAtUtc, Guid? CompletedBy, string? CompletedByName, DateTime CreatedAtUtc);

    public sealed record CommentMentionDto(Guid UserId, string UserName);

    public sealed record GetTaskCommentsResponseDto(IReadOnlyList<TaskCommentItemDto> Items, int TotalCount, int Page, int PageSize);

    public sealed record TaskCommentItemDto(long Id, Guid AuthorUserId, string AuthorUserName, string Content,
        DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, IReadOnlyList<CommentMentionDto> Mentions);

    public sealed record GetMyCommentsResponseDto(IReadOnlyList<MyCommentItemDto> Items, int TotalCount, int Page, int PageSize);

    public sealed record MyCommentItemDto(long Id, long TaskId, string TaskTitle, string Content, DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc, IReadOnlyList<CommentMentionDto> Mentions);

    public sealed record GetMyMentionsResponseDto(IReadOnlyList<MyMentionItemDto> Items, int TotalCount, int Page, int PageSize);

    public sealed record MyMentionItemDto(long CommentId, long TaskId, string TaskTitle, Guid AuthorUserId, string AuthorUserName,
        string Content, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

    public sealed record GetMyTasksResponseDto(IReadOnlyList<GetMyTasksItemDto> Items, int TotalCount, int Page, int PageSize);

    public sealed record GetMyTasksItemDto(long Id, Guid ProjectId, Guid AssigneeUserId, string AssigneeUserName, string ProjectName,
        string Title, string? Description, TaskItemStatus Status, TaskPriority Priority, Guid CreatedBy, string CreatorName,
        DateOnly? StartDate, DateOnly? DueDate, DateTime? CompletedAtUtc, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc,
        IReadOnlyList<TaskLabelResponseDto> Labels);

    public sealed record GetTeamTasksResponseDto(IReadOnlyList<GetTeamTasksItemDto> Items, int TotalCount, int Page, int PageSize);

    public sealed record GetTeamTasksItemDto(long Id, Guid ProjectId, string ProjectName, Guid? AssigneeUserId, string? AssigneeUserName,
        string Title, string? Description, TaskItemStatus Status, TaskPriority Priority, Guid CreatedBy, string CreatorName,
        DateOnly? StartDate, DateOnly? DueDate, DateTime? CompletedAtUtc, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc,
        IReadOnlyList<TaskLabelResponseDto> Labels);
}
