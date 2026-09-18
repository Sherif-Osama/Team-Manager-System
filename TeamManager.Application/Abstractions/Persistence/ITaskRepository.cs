using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface ITaskRepository
    {
        Task AddAsync(TaskItem task, CancellationToken cancellationToken);
    }
}