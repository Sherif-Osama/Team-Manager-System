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

            var users = await context.Users.Where(x => !x.IsActive && x.DeletedAtUtc ==
            null && x.UpdatedAtUtc <= cutoffDate && (x.LastLoginUtc == null ||
            x.LastLoginUtc <= cutoffDate)).ToListAsync(cancellationToken);

            if (users.Count == 0)
                return;

            var userIds = users.Select(x => x.Id).ToList();

            await context.TeamMembers.Where(x => userIds.Contains(x.UserId) &&
                    x.Status == TeamMemberStatus.Active &&
                    x.TeamRole != TeamRole.Owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, TeamMemberStatus.Removed)
                    .SetProperty(x => x.RemovedAtUtc, DateTime.UtcNow), cancellationToken);

            await context.RefreshTokens.Where(x => userIds.Contains(x.UserId) && x.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, DateTime.UtcNow), cancellationToken);

            foreach (var user in users)
            {
                user.SoftDelete();
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}