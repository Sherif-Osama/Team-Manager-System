using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
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
        #endregion
        public Task LinkPendingInvitationsToUserAsync(string email, Guid userId, CancellationToken cancellationToken)
        {
            return context.TeamInvitations.Where(x => x.InvitedEmail == email && x.Status == TeamInvitationStatus.Pending
            && x.InvitedUserId == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.InvitedUserId, userId), cancellationToken);
        }

        #region Ensure methods
        public Task<bool> HasActiveRoleAsync(Guid teamId, Guid userId, IReadOnlyCollection<TeamRole> roles, CancellationToken cancellationToken)
        {
            return context.TeamMembers
            .AnyAsync(m => m.TeamId == teamId && m.UserId == userId && m.Team.DeletedAtUtc == null
            && m.User.IsActive
            && m.Status == TeamMemberStatus.Active && roles.Contains(m.TeamRole), cancellationToken);
        }

        public Task<bool> HasActiveOwnedTeamsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return context.Teams.AnyAsync(t => t.OwnerUserId == userId && t.DeletedAtUtc == null, cancellationToken);
        }
        #endregion
    }
}