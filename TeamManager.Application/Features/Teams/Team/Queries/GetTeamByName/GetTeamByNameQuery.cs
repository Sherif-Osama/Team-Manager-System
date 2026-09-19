using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeamByName
{
    public sealed record GetTeamByNameQuery(string Name) : IRequest<GetTeamByNameResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageTeams;
    }
}