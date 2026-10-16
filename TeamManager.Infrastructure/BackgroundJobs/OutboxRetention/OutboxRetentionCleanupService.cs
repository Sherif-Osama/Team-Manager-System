using Microsoft.EntityFrameworkCore;
using TeamManager.Infrastructure.Persistence;

namespace TeamManager.Infrastructure.BackgroundJobs.OutboxRetention
{
    public sealed class OutboxRetentionCleanupService(TeamManagerDbContext context)
    {
        private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(7);

        public async Task<int> CleanupAsync(CancellationToken cancellationToken)
        {
            var retentionCutoffUtc = DateTime.UtcNow.Subtract(RetentionPeriod);

            return await context.OutboxMessages
                .Where(message =>
                    (message.ProcessedOnUtc != null && message.ProcessedOnUtc < retentionCutoffUtc) ||
                    (message.FailedOnUtc != null && message.FailedOnUtc < retentionCutoffUtc))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
