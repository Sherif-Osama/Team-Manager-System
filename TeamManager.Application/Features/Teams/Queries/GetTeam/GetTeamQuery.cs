using MediatR;

namespace TeamManager.Application.Features.Teams.Queries.GetTeam
{
    public sealed record GetTeamQuery(Guid TeamId) : IRequest<GetTeamResponse>;
}