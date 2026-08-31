using MediatR;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeams
{
    public sealed class GetTeamsQueryHandler(ITeamRepository teamRepository)
        : IRequestHandler<GetTeamsQuery, GetTeamsResponse>
    {
        public async Task<GetTeamsResponse> Handle(GetTeamsQuery request, CancellationToken cancellationToken)
        {
            return await teamRepository.GetPagedAsync(request, cancellationToken);
        }
    }
}
