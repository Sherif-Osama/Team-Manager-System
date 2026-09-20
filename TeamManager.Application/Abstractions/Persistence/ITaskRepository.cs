using TeamManager.Application.Common.Authorization.AuthorizationInfo;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface ITaskRepository
    {
        Task AddAsync(TaskItem task, CancellationToken cancellationToken);
        Task<TaskItem?> GetByIdAsync(long taskId, CancellationToken cancellationToken);
        Task<IReadOnlyList<string>> GetConflictingWithProjectDatesAsync(Guid projectId, DateOnly? projectStartDate, DateOnly? projectDueDate,
            int maxResults, CancellationToken cancellationToken);
        Task<TaskAuthorizationInfo?> GetAuthorizationInfoAsync(long taskId, Guid userId, ProjectRole[] requiredRoles, CancellationToken cancellationToken);
    }
}