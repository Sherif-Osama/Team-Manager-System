using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Features.Teams.Queries.GetTeam;
using TeamManager.Application.Features.Teams.Queries.GetTeams;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class TeamRepository : ITeamRepository
    {
        private readonly TeamManagerDbContext _context;

        public TeamRepository(TeamManagerDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Team team, CancellationToken cancellationToken)
        {
            await _context.Teams.AddAsync(team, cancellationToken);
        }

        public Task<GetTeamResponse?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken)
        {
            return _context.Teams.AsNoTracking().Where(x => x.Id == teamId && x.DeletedAtUtc == null)
                .Select(x => new GetTeamResponse(x.Id, x.Name, x.Description, x.IsActive, x.OwnerUserId,
                x.Owner.DisplayName, x.CreatedBy, x.Creator.DisplayName, x.Members.Count,
                x.Projects.Count, x.CreatedAtUtc, x.UpdatedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<Team?> GetByIdForUpdateAsync(Guid teamId, CancellationToken cancellationToken)
        {
            return _context.Teams.FirstOrDefaultAsync(T => T.Id == teamId && T.DeletedAtUtc == null, cancellationToken);
        }

        public Task<Team?> GetByNameAsync(string name, CancellationToken cancellationToken)
        {
            return _context.Teams.FirstOrDefaultAsync(x => x.Name == name && x.DeletedAtUtc == null, cancellationToken);
        }

        public async Task<GetTeamsResponse> GetPagedAsync(string? search, bool? isActive, int page,
            int pageSize, CancellationToken cancellationToken)
        {
            var query = _context.Teams.AsNoTracking().Where(x => x.DeletedAtUtc == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x => x.Name.Contains(search));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
              .OrderByDescending(x => x.CreatedAtUtc)
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .Select(x => new TeamListItem(x.Id, x.Name, x.Description, x.OwnerUserId, x.Owner.DisplayName,
              x.CreatedBy, x.Creator.DisplayName, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetTeamsResponse(items, page, pageSize, totalCount);
        }
    }
}