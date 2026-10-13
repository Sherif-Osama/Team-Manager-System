using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.ActivityLog.Queries.GetProjectActivity
{
    public sealed record GetProjectActivityQuery(Guid ProjectId, int Page = 1, int PageSize = 20) : IRequest<GetProjectActivityResponse>, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Viewer, ProjectRole.Member];
    }
}