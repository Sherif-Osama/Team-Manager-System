using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface ITaskRepository
    {
        Task AddAsync(TaskItem task, CancellationToken cancellationToken);
        Task<TaskItem?> GetByIdAsync(long taskId, CancellationToken cancellationToken);
        Task<IReadOnlyList<string>> GetConflictingWithProjectDatesAsync(Guid projectId, DateOnly? projectStartDate, DateOnly? projectDueDate,
            int maxResults, CancellationToken cancellationToken);
    }
}