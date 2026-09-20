using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProject
{
    public sealed record GetProjectQuery(Guid ProjectId) : IRequest<GetProjectResponse>, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Viewer, ProjectRole.Member];
    }
}