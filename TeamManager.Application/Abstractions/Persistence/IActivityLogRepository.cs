namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IActivityLogRepository
    {
        Task AddAsync(Domain.Entities.ActivityLog activityLog, CancellationToken cancellationToken);
    }
}