using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetMyTasks
{
    public sealed record GetMyTasksResponse(IReadOnlyCollection<GetMyTasksItem> Items, int TotalCount, int Page = 1,
        int PageSize = 20);

    public sealed record GetMyTasksItem(long Id, Guid ProjectId, Guid AssigneeUserId, string AssigneeUserName, string ProjectName, string Title, string? Description,
       TaskItemStatus Status, TaskPriority Priority, Guid CreatedBy, string CreatorName, DateOnly? StartDate,
        DateOnly? DueDate, DateTime? CompletedAtUtc, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
}