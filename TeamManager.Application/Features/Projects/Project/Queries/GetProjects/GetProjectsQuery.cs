using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProjects
{
    public sealed record GetProjectsQuery(string? Search, ProjectStatus? Status, int Page = 1, int PageSize = 20)
        : IRequest<GetProjectsResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageProjects;
    }
}