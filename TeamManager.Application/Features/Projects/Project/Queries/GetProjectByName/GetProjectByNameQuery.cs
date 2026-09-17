using MediatR;
using TeamManager.Application.Abstractions.DefaultValues;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProjectByName
{
    public sealed record GetProjectByNameQuery(string Name) : IRequest<GetProjectByNameResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageProjects;
    }
}