using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class TaskRepository(TeamManagerDbContext context) : ITaskRepository
    {
        public async Task AddAsync(TaskItem newTask, CancellationToken cancellationToken)
        {
            await context.AddAsync(newTask, cancellationToken);
        }

        public async Task<IReadOnlyList<string>> GetConflictingWithProjectDatesAsync(Guid projectId, DateOnly? projectStartDate,
            DateOnly? projectDueDate, int maxResults, CancellationToken cancellationToken)
        {
            return await context.Tasks.AsNoTracking().Where(t => t.ProjectId == projectId && t.DeletedAtUtc == null
            && t.Status != TaskItemStatus.Cancelled)
                .Where(t => (projectStartDate.HasValue && t.StartDate.HasValue && t.StartDate.Value < projectStartDate.Value)
                || (projectDueDate.HasValue && t.DueDate.HasValue && t.DueDate.Value > projectDueDate.Value)).OrderBy(t => t.Id)
                .Select(t => t.Title).Take(maxResults).ToListAsync(cancellationToken);
        }
    }
}