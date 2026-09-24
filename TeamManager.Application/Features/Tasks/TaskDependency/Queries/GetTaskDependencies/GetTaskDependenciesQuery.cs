using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskDependency.Queries.GetTaskDependencies
{
    public sealed record GetTaskDependenciesQuery(long TaskId, int Page = 1, int PageSize = 20)
        : IRequest<GetTaskDependenciesResponse>, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member, ProjectRole.Viewer];
    }
}