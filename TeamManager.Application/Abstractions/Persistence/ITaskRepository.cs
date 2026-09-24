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
        Task UnassignActiveTasksAsync(Guid userId, CancellationToken cancellationToken);
        Task UnassignActiveTasksByTeamAsync(Guid teamId, Guid userId, CancellationToken cancellationToken);
        Task UnassignActiveTasksByProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken);
        Task<bool> WouldCreateDependencyCycleAsync(Guid projectId, long taskId, long dependsOnTaskId, CancellationToken cancellationToken);
        Task<IReadOnlyList<string>> GetIncompleteDependencyTitlesAsync(long taskId, CancellationToken cancellationToken);
        Task<IReadOnlyList<(string Title, DateOnly DueDate)>> GetDependentsViolatingDueDateAsync(long taskId, DateOnly newDueDate, CancellationToken cancellationToken);
    }
}