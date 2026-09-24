using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskDependency.Queries.GetTaskDependencies
{
    public sealed record GetTaskDependenciesResponse(IReadOnlyCollection<TaskDependencyItem> Items, int TotalCount,
        int Page = 1, int PageSize = 20);

    public sealed record TaskDependencyItem(long DependencyId, long TaskId, long DependsOnTaskId, string DependsOnTaskTitle,
        TaskItemStatus DependsOnTaskStatus, TaskPriority DependsOnTaskPriority, DateOnly? DependsOnTaskDueDate,
        Guid CreatedBy, string CreatedByName, DateTime CreatedAtUtc);
}