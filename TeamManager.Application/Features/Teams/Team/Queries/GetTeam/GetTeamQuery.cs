using MediatR;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeam
{
    public sealed record GetTeamQuery(Guid TeamId) : IRequest<GetTeamResponse>;
}