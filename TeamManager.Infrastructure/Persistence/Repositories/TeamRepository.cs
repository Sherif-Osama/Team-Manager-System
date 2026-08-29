using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Features.Teams.Team.Queries.GetTeam;
using TeamManager.Application.Features.Teams.Team.Queries.GetTeams;
using TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMember;
using TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMembers;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class TeamRepository(TeamManagerDbContext context) : ITeamRepository
    {

        public async Task AddAsync(Team team, CancellationToken cancellationToken)
        {
            await context.Teams.AddAsync(team, cancellationToken);
        }

        public Task<GetTeamResponse?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken)
        {
            return context.Teams.AsNoTracking().Where(x => x.Id == teamId && x.DeletedAtUtc == null)
                .Select(x => new GetTeamResponse(x.Id, x.Name, x.Description, x.IsActive, x.OwnerUserId,
                x.Owner.DisplayName, x.CreatedBy, x.Creator.DisplayName, x.Members.Count,
                x.Projects.Count, x.CreatedAtUtc, x.UpdatedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<Team?> GetByIdForUpdateAsync(Guid teamId, CancellationToken cancellationToken)
        {
            return context.Teams.FirstOrDefaultAsync(T => T.Id == teamId && T.DeletedAtUtc == null, cancellationToken);
        }

        public Task<Team?> GetByNameAsync(string name, CancellationToken cancellationToken)
        {
            return context.Teams.FirstOrDefaultAsync(x => x.Name == name && x.DeletedAtUtc == null, cancellationToken);
        }
        public async Task<GetMembersResponse> GetMembersAsync(Guid teamId, int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = context.TeamMembers.AsNoTracking().Where(x => x.TeamId == teamId && x.Status == TeamMemberStatus.Active);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query.OrderBy(x => x.JoinedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new TeamMemberItem(
                    x.Id, x.UserId, x.User.DisplayName, x.User.Email,
                    x.TeamRole, x.Status, x.JoinedAtUtc)).ToListAsync(cancellationToken);

            return new GetMembersResponse(items, page, pageSize, totalCount);
        }
        public async Task<GetTeamsResponse> GetPagedAsync(string? search, bool? isActive, int page,
            int pageSize, CancellationToken cancellationToken)
        {
            var query = context.Teams.AsNoTracking().Where(x => x.DeletedAtUtc == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x => x.Name.Contains(search));
            }

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);


            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
              .OrderByDescending(x => x.CreatedAtUtc)
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .Select(x => new TeamListItem(x.Id, x.Name, x.Description, x.OwnerUserId, x.Owner.DisplayName,
              x.CreatedBy, x.Creator.DisplayName, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetTeamsResponse(items, page, pageSize, totalCount);
        }
        public Task<bool> ExistsAsync(Guid teamId, CancellationToken cancellationToken)
        {
            return context.Teams.AnyAsync(T => T.Id == teamId, cancellationToken);
        }
        public Task<GetMemberResponse?> GetMemberAsync(Guid teamId, long memberId, CancellationToken cancellationToken)
        {
            return context.TeamMembers.AsNoTracking().Where(x => x.TeamId == teamId && x.Id == memberId)
                .Select(x => new GetMemberResponse(x.Id, x.UserId, x.User.DisplayName, x.User.Email, x.TeamRole,
                x.Status, x.JoinedAtUtc, x.InvitedBy, x.InvitedByUser != null ? x.InvitedByUser.DisplayName : null,
                x.RemovedAtUtc, x.RemovedBy, x.RemovedByUser != null ? x.RemovedByUser.DisplayName : null))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<Team?> GetByIdWithMembersAsync(Guid teamId, CancellationToken cancellationToken)
        {
            return context.Teams.Include(x => x.Members).FirstOrDefaultAsync(x => x.Id == teamId &&
            x.DeletedAtUtc == null, cancellationToken);
        }

        public Task<TeamMember?> GetMemberForUpdateAsync(Guid teamId, long memberId, CancellationToken cancellationToken)
        {
            return context.TeamMembers.FirstOrDefaultAsync(x => x.TeamId == teamId && x.Id == memberId, cancellationToken);
        }

        public Task<bool> HasActiveRoleAsync(Guid teamId, Guid userId, IReadOnlyCollection<TeamRole> roles, CancellationToken cancellationToken)
        {
            return context.TeamMembers
            .AnyAsync(m => m.TeamId == teamId && m.UserId == userId
            &&
            m.Status == TeamMemberStatus.Active && roles.Contains(m.TeamRole), cancellationToken);
        }

        public Task<TeamMember?> GetMemberByUserIdAsync(Guid teamId, Guid userId, CancellationToken cancellationToken)
        {
            return context.TeamMembers.FirstOrDefaultAsync(x => x.TeamId == teamId && x.UserId == userId
            && x.Status == TeamMemberStatus.Active, cancellationToken);
        }
    }
}