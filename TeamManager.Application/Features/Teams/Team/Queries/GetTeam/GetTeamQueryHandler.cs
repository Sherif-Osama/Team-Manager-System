using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeam
{
    public sealed class GetTeamQueryHandler(ITeamRepository teamRepository)
        : IRequestHandler<GetTeamQuery, GetTeamResponse>
    {
        public async Task<GetTeamResponse> Handle(GetTeamQuery request, CancellationToken cancellationToken)
        {
            var team = await teamRepository.GetByIdAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            return team;
        }
    }
}