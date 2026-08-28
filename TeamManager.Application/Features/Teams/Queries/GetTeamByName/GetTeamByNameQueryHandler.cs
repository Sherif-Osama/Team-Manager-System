using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Queries.GetTeamByName
{
    public sealed class GetTeamByNameQueryHandler : IRequestHandler<GetTeamByNameQuery, GetTeamByNameResponse>
    {
        ITeamRepository _teamRepository;
        public GetTeamByNameQueryHandler(ITeamRepository teamRepository)
        {
            _teamRepository = teamRepository;
        }

        public async Task<GetTeamByNameResponse> Handle(GetTeamByNameQuery request, CancellationToken cancellationToken)
        {
            var team = await _teamRepository.GetByNameAsync(
              request.Name,
              cancellationToken);

            if (team is null)
                throw new TeamNotFoundException(request.Name);

            return new GetTeamByNameResponse(team.Id, team.Name, team.Description,
                team.IsActive, team.OwnerUserId, team.CreatedBy, team.CreatedAtUtc);
        }
    }
}