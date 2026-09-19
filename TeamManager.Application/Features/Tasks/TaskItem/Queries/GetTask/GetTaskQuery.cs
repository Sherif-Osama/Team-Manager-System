using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTask
{
    public sealed record GetTaskQuery(Guid ProjectId, long TaskId) : IRequest<GetTaskResponse>, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member, ProjectRole.Viewer];
    }
}