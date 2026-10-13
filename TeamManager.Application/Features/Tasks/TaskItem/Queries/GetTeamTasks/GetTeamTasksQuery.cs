using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTeamTasks
{
    public sealed record GetTeamTasksQuery(Guid TeamId, string? Search, Guid? ProjectId, TaskItemStatus? Status, TaskPriority? Priority, int Page = 1, int PageSize = 20) : IRequest<GetTeamTasksResponse>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Admin, TeamRole.Member];
    }
}