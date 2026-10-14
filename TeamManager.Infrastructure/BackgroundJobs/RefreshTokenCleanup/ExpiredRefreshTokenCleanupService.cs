using Microsoft.EntityFrameworkCore;
using TeamManager.Infrastructure.Persistence;

namespace TeamManager.Infrastructure.BackgroundJobs.RefreshTokenCleanup
{
    public sealed class ExpiredRefreshTokenCleanupService(TeamManagerDbContext context)
    {
        private const int RetentionDays = 30;

        public async Task<int> CleanupAsync(CancellationToken cancellationToken)
        {
            var cutoffUtc = DateTime.UtcNow.AddDays(-RetentionDays);
            var totalDeleted = 0;

            while (true)
            {
                var deletedCount = await context.RefreshTokens.Where(x => x.ExpiresAtUtc <= cutoffUtc)
                    .Where(x => !context.RefreshTokens.Any(other => other.ReplacedByTokenId == x.Id))
                    .ExecuteDeleteAsync(cancellationToken);

                if (deletedCount == 0)
                    break;

                totalDeleted += deletedCount;
            }

            return totalDeleted;
        }
    }
}