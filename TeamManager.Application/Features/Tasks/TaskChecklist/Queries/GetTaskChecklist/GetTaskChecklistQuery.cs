using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Queries.GetTaskChecklist
{
    public sealed record GetTaskChecklistQuery(long TaskId) : IRequest<IReadOnlyCollection<GetTaskChecklistResponse>>,
          ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member, ProjectRole.Viewer];
    }
}
