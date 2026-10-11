namespace TeamManager.Application.Abstractions.ActivityLog
{
    public interface IActivityLogWriter
    {
        Task WriteProjectActivityAsync(Guid projectId, Guid actorUserId, string activityType, string entityType, string entityId,
            string? metadata, CancellationToken cancellationToken);
    }
}