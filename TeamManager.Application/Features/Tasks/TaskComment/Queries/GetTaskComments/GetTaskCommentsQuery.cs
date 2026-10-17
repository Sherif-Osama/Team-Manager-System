using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskComment.Queries.GetTaskComments
{
    public sealed record GetTaskCommentsQuery(long TaskId, int Page = 1, int PageSize = 20) : IRequest<GetTaskCommentsResponse>,
        ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member, ProjectRole.Viewer];
    }
}