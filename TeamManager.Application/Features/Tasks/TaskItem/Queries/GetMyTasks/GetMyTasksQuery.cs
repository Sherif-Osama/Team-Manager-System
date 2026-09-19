using MediatR;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetMyTasks
{
    public sealed record GetMyTasksQuery(string? Search, Guid? ProjectId, TaskItemStatus? Status, TaskPriority? Priority,
        int Page = 1, int PageSize = 20) : IRequest<GetMyTasksResponse>;
}