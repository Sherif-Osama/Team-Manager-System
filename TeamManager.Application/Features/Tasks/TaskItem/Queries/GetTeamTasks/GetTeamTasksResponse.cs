using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTeamTasks
{
    public sealed record GetTeamTasksResponse(IReadOnlyCollection<GetTeamTasksItem> Items, int TotalCount, int Page = 1,
    int PageSize = 20);

    public sealed record GetTeamTasksItem(long Id, Guid ProjectId, string ProjectName, Guid? AssigneeUserId, string? AssigneeUserName, string Title, string? Description, TaskItemStatus Status,
        TaskPriority Priority, Guid CreatedBy, string CreatorName, DateOnly? StartDate, DateOnly? DueDate,
        DateTime? CompletedAtUtc, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, IReadOnlyCollection<GetTeamTaskLabelResponse> Labels);

    public sealed record GetTeamTaskLabelResponse(long Id, string Name, string ColorHex);
}