using Microsoft.EntityFrameworkCore;
using TeamManager.Domain.Enums;
using TeamManager.Infrastructure.Persistence;

namespace TeamManager.Infrastructure.BackgroundJobs.DeleteInactiveUsers
{
    public sealed class InactiveUserDeletionService(TeamManagerDbContext context)
    {
        public async Task DeleteInactiveUsersAsync(CancellationToken cancellationToken)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30);

            // soft delete users that are inactive, not deleted, and have not logged in for 30 days or more
            var users = await context.Users.Where(x => !x.IsActive && x.DeletedAtUtc ==
            null && x.UpdatedAtUtc <= cutoffDate && (x.LastLoginUtc == null ||
            x.LastLoginUtc <= cutoffDate)).ToListAsync(cancellationToken);

            if (users.Count == 0)
                return;

            var candidateForDeletionIds = users.Select(x => x.Id).ToList();

            var ownerIdsWithActiveTeams = await context.Teams.Where(t => candidateForDeletionIds.Contains(t.OwnerUserId)
            && t.DeletedAtUtc == null).Select(t => t.OwnerUserId).Distinct().ToListAsync(cancellationToken);

            var ownerIdsWithActiveProjects = await context.Projects.
                Where(p => candidateForDeletionIds.Contains(p.OwnerUserId)
                && p.DeletedAtUtc == null)
                .Select(p => p.OwnerUserId).Distinct().ToListAsync(cancellationToken);


            users = users.Where(u => !ownerIdsWithActiveTeams.Contains(u.Id) && !ownerIdsWithActiveProjects.Contains(u.Id)).ToList();


            if (users.Count == 0)
                return;

            var userIds = users.Select(x => x.Id).ToList();

            await context.ExecuteInTransactionAsync(async ct =>
            {
                await context.TeamMembers.Where(x => userIds.Contains(x.UserId) &&
                (x.Status == TeamMemberStatus.Active || x.Status == TeamMemberStatus.Suspended) &&
                x.TeamRole != TeamRole.Owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, TeamMemberStatus.Removed)
                .SetProperty(x => x.RemovedAtUtc, DateTime.UtcNow), ct);

                await context.RefreshTokens.Where(x => userIds.Contains(x.UserId) && x.RevokedAtUtc == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, DateTime.UtcNow), ct);

                foreach (var user in users)
                {
                    user.SoftDelete();
                }

            }, cancellationToken);
        }
    }
}