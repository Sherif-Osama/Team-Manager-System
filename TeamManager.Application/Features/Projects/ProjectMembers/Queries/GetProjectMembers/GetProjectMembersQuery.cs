using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Queries.GetProjectMembers
{
    public sealed record GetProjectMembersQuery(Guid ProjectId, string? Search, ProjectRole? Role, ProjectMemberStatus? Status,
        int Page = 1, int PageSize = 20) : IRequest<GetProjectMembersResponse>, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member, ProjectRole.Viewer];
    }
}