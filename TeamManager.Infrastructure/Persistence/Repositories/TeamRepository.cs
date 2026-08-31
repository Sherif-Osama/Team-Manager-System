using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Features.Teams.Invitations.Queries.GetInvitations;
using TeamManager.Application.Features.Teams.Invitations.Queries.GetMyInvitations;
using TeamManager.Application.Features.Teams.Invitations.Queries.GetUserInvitations;
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

        #region Get Methods
        public Task<GetTeamResponse?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken)
        {
            return context.Teams.AsNoTracking().Where(x => x.Id == teamId && x.DeletedAtUtc == null)
                .Select(x => new GetTeamResponse(x.Id, x.Name, x.Description, x.IsActive, x.OwnerUserId,
                x.Owner.DisplayName, x.CreatedBy, x.Creator.DisplayName, x.Members.Count,
                x.Projects.Count, x.CreatedAtUtc, x.UpdatedAtUtc))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<GetInvitationsByEmailResponse> GetInvitationsByEmailAsync(GetInvitationsByEmailQuery query, CancellationToken cancellationToken)
        {
            var invitations = context.TeamInvitations.AsNoTracking().Where(x => x.TeamId == query.TeamId && x.InvitedEmail == query.Email);

            if (query.Status.HasValue)
                invitations = invitations.Where(x => x.Status == query.Status.Value);


            var totalCount = await invitations.CountAsync(cancellationToken);

            var items = await invitations
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new InvitationItem(x.Id, x.TeamId, x.Team.Name, x.InvitedEmail, x.InvitedUserId, x.TeamRole,
                x.Status, x.ExpiresAtUtc, x.AcceptedAtUtc, x.RejectedAtUtc, x.CancelledAtUtc,
                x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetInvitationsByEmailResponse(items, query.Page, query.PageSize, totalCount);
        }

        public async Task<GetMyInvitationsResponse> GetMyInvitationsAsync(string email, GetMyInvitationsQuery query, CancellationToken cancellationToken)
        {
            var invitations = context.TeamInvitations.AsNoTracking().Where(x => x.InvitedEmail == email);

            if (query.Status.HasValue)
                invitations = invitations.Where(x => x.Status == query.Status.Value);

            var totalCount = await invitations.CountAsync(cancellationToken);

            var items = await invitations
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new MyInvitationItem(x.Id, x.TeamId, x.Team.Name, x.InvitedEmail, x.InvitedUserId, x.TeamRole,
                x.Status, x.ExpiresAtUtc, x.AcceptedAtUtc, x.RejectedAtUtc,
                x.CancelledAtUtc, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetMyInvitationsResponse(items, query.Page, query.PageSize, totalCount);
        }

        public Task<Team?> GetByIdWithInvitationsAsync(Guid teamId, CancellationToken cancellationToken)
        {
            return context.Teams.Include(t => t.Invitations)
                .FirstOrDefaultAsync(t => t.Id == teamId && t.DeletedAtUtc == null, cancellationToken);
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

            var items = await query.OrderBy(x => x.JoinedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).
                Select(x => new TeamMemberItem(
                    x.Id, x.UserId, x.User.DisplayName, x.User.Email,
                    x.TeamRole, x.Status, x.JoinedAtUtc)).ToListAsync(cancellationToken);

            return new GetMembersResponse(items, page, pageSize, totalCount);
        }
        public async Task<GetTeamsResponse> GetPagedAsync(GetTeamsQuery request, CancellationToken cancellationToken)
        {
            var query = context.Teams.AsNoTracking().Where(x => x.DeletedAtUtc == null);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(x => x.Name.Contains(request.Search.Trim()));

            if (request.IsActive.HasValue)
                query = query.Where(x => x.IsActive == request.IsActive.Value);


            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
              .OrderByDescending(x => x.CreatedAtUtc)
              .Skip((request.Page - 1) * request.PageSize)
              .Take(request.PageSize)
              .Select(x => new TeamListItem(x.Id, x.Name, x.Description, x.OwnerUserId, x.Owner.DisplayName,
              x.CreatedBy, x.Creator.DisplayName, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetTeamsResponse(items, request.Page, request.PageSize, totalCount);
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


        public Task<Team?> GetByIdWithMembersAndInvitationsAsync(Guid teamId, CancellationToken cancellationToken)
        {
            return context.Teams
                .Include(t => t.Members
                    .Where(m => m.Status == TeamMemberStatus.Active))
                .Include(t => t.Invitations.Where(i => i.Status == TeamInvitationStatus.Pending))
                .FirstOrDefaultAsync(t => t.Id == teamId && t.DeletedAtUtc == null, cancellationToken);
        }

        public Task<Team?> GetByInvitationTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return context.Teams.Include(t => t.Invitations.Where(i => i.TokenHash == tokenHash))
                .FirstOrDefaultAsync(t => t.Invitations.Any(i => i.TokenHash == tokenHash)
                &&
                t.DeletedAtUtc == null, cancellationToken);
        }

        public async Task<GetInvitationsResponse> GetInvitationsAsync(
            GetInvitationsQuery query, CancellationToken cancellationToken)
        {
            var invitationsQuery = context.TeamInvitations.AsNoTracking().Where(x => x.TeamId == query.TeamId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();

                invitationsQuery = invitationsQuery.Where(x =>
                    x.InvitedEmail.Contains(search));
            }

            if (query.Status.HasValue)
                invitationsQuery = invitationsQuery.Where(x =>
                    x.Status == query.Status.Value);

            if (query.role.HasValue)
                invitationsQuery = invitationsQuery.Where(x =>
                    x.TeamRole == query.role.Value);

            var totalCount = await invitationsQuery.CountAsync(cancellationToken);

            var items = await invitationsQuery
                .OrderByDescending(x => x.CreatedAtUtc).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
                .Select(x => new TeamInvitationItem(x.Id, x.Team.Name, x.InvitedEmail, x.InvitedUserId,
                x.InvitedBy, x.TeamRole, x.Status, x.ExpiresAtUtc,
                x.AcceptedAtUtc, x.RejectedAtUtc,
                x.CancelledAtUtc, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetInvitationsResponse(items, query.Page, query.PageSize, totalCount);
        }
        #endregion

        #region Ensure methods
        public Task<bool> ExistsAsync(Guid teamId, CancellationToken cancellationToken)
        {
            return context.Teams.AnyAsync(T => T.Id == teamId && T.DeletedAtUtc == null, cancellationToken);
        }

        public Task<bool> HasActiveRoleAsync(Guid teamId, Guid userId, IReadOnlyCollection<TeamRole> roles, CancellationToken cancellationToken)
        {
            return context.TeamMembers
            .AnyAsync(m => m.TeamId == teamId && m.UserId == userId
            && m.Team.IsActive && m.Team.DeletedAtUtc == null
            && m.User.IsActive
            && m.Status == TeamMemberStatus.Active && roles.Contains(m.TeamRole), cancellationToken);
        }
        #endregion
    }
}