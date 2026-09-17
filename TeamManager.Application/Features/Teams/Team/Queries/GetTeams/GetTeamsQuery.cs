using MediatR;
using TeamManager.Application.Abstractions.DefaultValues;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeams
{
    public sealed record GetTeamsQuery(string? Search, bool? IsActive, int Page = 1, int PageSize = 20) : IRequest<GetTeamsResponse>
        , IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageTeams;
    }
}