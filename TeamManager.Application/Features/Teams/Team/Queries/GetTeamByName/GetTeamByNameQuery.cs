using MediatR;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeamByName
{
    public sealed record GetTeamByNameQuery(string Name) : IRequest<GetTeamByNameResponse>;
}