using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization.AuthorizationInfo;
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

        public async Task<TaskItem?> GetByIdAsync(long taskId, CancellationToken cancellationToken)
        {
            var task = await context.Tasks.FindAsync([taskId], cancellationToken);

            return task is null || task.DeletedAtUtc is not null ? null : task;
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

        public async Task<TaskAuthorizationInfo?> GetAuthorizationInfoAsync(long taskId, Guid userId, ProjectRole[] requiredRoles, CancellationToken cancellationToken)
        {
            return await context.Tasks.AsNoTracking().Where(t => t.Id == taskId && t.DeletedAtUtc == null
            && t.Project.DeletedAtUtc == null).Select(t => new TaskAuthorizationInfo(true,
            t.Project.Members.Any(pm => pm.UserId == userId && pm.Status == ProjectMemberStatus.Active && pm.User.IsActive
            && requiredRoles.Contains(pm.ProjectRole)), t.Project.Team.Members.Any(tm => tm.UserId == userId &&
            tm.Status == TeamMemberStatus.Active && tm.User.IsActive && tm.TeamRole == TeamRole.Owner
            && tm.Team.DeletedAtUtc == null))).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UnassignActiveTasksAsync(Guid userId, CancellationToken cancellationToken)
        {
            await context.Tasks.Where(t => t.AssigneeUserId == userId && t.DeletedAtUtc == null && t.Status != TaskItemStatus.Done)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.AssigneeUserId, (Guid?)null)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow).SetProperty(x => x.Status,
                x => x.Status == TaskItemStatus.InProgress ? TaskItemStatus.Todo : x.Status), cancellationToken);
        }
    }
}