using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Queries.GetTeam
{
    public sealed class GetTeamQueryHandler : IRequestHandler<GetTeamQuery, GetTeamResponse>
    {
        private readonly ITeamRepository _teamRepository;

        public GetTeamQueryHandler(ITeamRepository teamRepository)
        {
            _teamRepository = teamRepository;
        }

        public async Task<GetTeamResponse> Handle(GetTeamQuery request, CancellationToken cancellationToken)
        {
            var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.TeamId);

            return team;
        }
    }
}