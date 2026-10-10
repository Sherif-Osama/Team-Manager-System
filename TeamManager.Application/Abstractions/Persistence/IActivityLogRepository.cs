using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IActivityLogRepository
    {
        Task AddAsync(ActivityLog activityLog, CancellationToken cancellationToken);
    }
}