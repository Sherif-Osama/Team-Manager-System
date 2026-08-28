using MediatR;

namespace TeamManager.Application.Features.Teams.Queries.GetTeamByName
{
    public sealed record GetTeamByNameQuery(string Name) : IRequest<GetTeamByNameResponse>;
}