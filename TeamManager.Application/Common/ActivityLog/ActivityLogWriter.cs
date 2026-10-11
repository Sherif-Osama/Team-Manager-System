using TeamManager.Application.Abstractions.ActivityLog;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;

namespace TeamManager.Application.Common.ActivityLog
{
    public sealed class ActivityLogWriter(IProjectRepository projectRepository, IActivityLogRepository activityLogRepository)
        : IActivityLogWriter
    {
        public async Task WriteProjectActivityAsync(Guid projectId, Guid actorUserId, string activityType, string entityType,
            string entityId, string? metadata, CancellationToken cancellationToken)
        {
            var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);

            if (project is null)
                throw new ProjectNotFoundException(projectId);

            var activityLog = new Domain.Entities.ActivityLog(project.TeamId, actorUserId, activityType, entityType, entityId,
                projectId, metadata);

            await activityLogRepository.AddAsync(activityLog, cancellationToken);
        }
    }
}