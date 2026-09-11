using MediatR;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeamByName
{
    public sealed record GetTeamByNameQuery(string Name) : IRequest<GetTeamByNameResponse>, IRequiresPermission
    {
        public string PermissionCode => "system.manage_Teams";
    }
}