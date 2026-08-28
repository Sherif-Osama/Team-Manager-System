using TeamManager.Application.Features.Teams.Queries.GetTeam;
using TeamManager.Application.Features.Teams.Queries.GetTeams;
using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface ITeamRepository
    {
        Task AddAsync(Team team, CancellationToken cancellationToken);

        Task<GetTeamResponse?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken);
        Task<Team?> GetByIdForUpdateAsync(Guid teamId, CancellationToken cancellationToken);
        Task<Team?> GetByNameAsync(string name, CancellationToken cancellationToken);
        Task<GetTeamsResponse> GetPagedAsync(string? search, bool? isActive, int page,
            int pageSize, CancellationToken cancellationToken);
    }
}
