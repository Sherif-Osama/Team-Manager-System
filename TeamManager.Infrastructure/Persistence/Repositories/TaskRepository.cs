using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class TaskRepository(TeamManagerDbContext context) : ITaskRepository
    {
        public async Task AddAsync(TaskItem newTask, CancellationToken cancellationToken)
        {
            await context.AddAsync(newTask, cancellationToken);
        }
    }
}