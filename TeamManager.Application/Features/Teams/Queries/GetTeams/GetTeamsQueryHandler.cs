using MediatR;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Teams.Queries.GetTeams
{
    public sealed class GetTeamsQueryHandler : IRequestHandler<GetTeamsQuery, GetTeamsResponse>
    {
        private readonly ITeamRepository _teamRepository;

        public GetTeamsQueryHandler(ITeamRepository teamRepository)
        {
            _teamRepository = teamRepository;
        }

        public async Task<GetTeamsResponse> Handle(GetTeamsQuery request, CancellationToken cancellationToken)
        {
            return await _teamRepository.GetPagedAsync(request.Search, request.IsActive, request.Page,
                request.PageSize, cancellationToken);
        }
    }
}
