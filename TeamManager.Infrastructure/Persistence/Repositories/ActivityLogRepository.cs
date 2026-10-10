using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class ActivityLogRepository(TeamManagerDbContext context) : IActivityLogRepository
    {
        public async Task AddAsync(ActivityLog activityLog, CancellationToken cancellationToken)
        {
            await context.ActivityLogs.AddAsync(activityLog, cancellationToken);
        }
    }
}