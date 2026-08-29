using MediatR;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeams
{
    public sealed record GetTeamsQuery(string? Search, bool? IsActive, int Page = 1, int PageSize = 20)
        : IRequest<GetTeamsResponse>;
}
