using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProjectByName
{
    public sealed record GetProjectByNameQuery(string Name) : IRequest<GetProjectByNameResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageProjects;
    }
}