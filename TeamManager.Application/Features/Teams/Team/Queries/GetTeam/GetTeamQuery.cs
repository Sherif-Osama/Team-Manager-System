using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeam
{
    public sealed record GetTeamQuery(Guid TeamId) : IRequest<GetTeamResponse>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => new[] { TeamRole.Member, TeamRole.Admin, TeamRole.Owner, TeamRole.Viewer };
    }
}