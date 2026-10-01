using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTeamTasks
{
    public sealed record GetTeamTasksRequest(Guid? ProjectId, string? Search, TaskItemStatus? Status, TaskPriority? Priority,
        int Page = 1, int PageSize = 20);
}