using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
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

        public Task UnassignActiveTasksAsync(Guid userId, CancellationToken cancellationToken)
            => UnassignActiveTasksAsync(t => t.AssigneeUserId == userId, cancellationToken);

        public Task UnassignActiveTasksByTeamAsync(Guid teamId, Guid userId, CancellationToken cancellationToken)
            => UnassignActiveTasksAsync(t => t.Project.TeamId == teamId && t.AssigneeUserId == userId, cancellationToken);

        public Task UnassignActiveTasksByProjectAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
            => UnassignActiveTasksAsync(t => t.ProjectId == projectId && t.AssigneeUserId == userId, cancellationToken);

        private Task UnassignActiveTasksAsync(Expression<Func<TaskItem, bool>> scope, CancellationToken cancellationToken)
        {
            return context.Tasks.Where(scope).Where(t => t.DeletedAtUtc == null && t.Status != TaskItemStatus.Done)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.AssigneeUserId, (Guid?)null)
                    .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow)
                    .SetProperty(x => x.Status, x => x.Status == TaskItemStatus.InProgress ? TaskItemStatus.Todo : x.Status),
                    cancellationToken);
        }

        public async Task<bool> WouldCreateDependencyCycleAsync(Guid projectId, long taskId, long dependsOnTaskId,
            CancellationToken cancellationToken)
        {
            var result = await context.Database.SqlQuery<bool>($"""
            WITH DependencyGraph AS
            (
                SELECT td.DependsOnTaskId FROM TaskDependencies td
                INNER JOIN Tasks t ON t.TaskId = td.TaskId
                INNER JOIN Tasks dependsOn ON dependsOn.TaskId = td.DependsOnTaskId
                WHERE td.TaskId = {dependsOnTaskId}    AND t.ProjectId = {projectId} AND t.DeletedAtUtc IS NULL AND 
                dependsOn.DeletedAtUtc IS NULL

                UNION ALL

                SELECT td.DependsOnTaskId FROM TaskDependencies td
                INNER JOIN DependencyGraph dg ON td.TaskId = dg.DependsOnTaskId
                INNER JOIN Tasks t  ON t.TaskId = td.TaskId
                INNER JOIN Tasks dependsOn ON dependsOn.TaskId = td.DependsOnTaskId
                WHERE t.ProjectId = {projectId}  AND t.DeletedAtUtc IS NULL AND dependsOn.DeletedAtUtc IS NULL
            )
            SELECT CAST(
                CASE
                    WHEN EXISTS (SELECT 1 FROM DependencyGraph WHERE DependsOnTaskId = {taskId})
                    THEN 1
                    ELSE 0
                END AS bit) 
                AS [Value]
            OPTION (MAXRECURSION 32767)
            """).ToListAsync(cancellationToken);

            return result.Single();
        }

        public async Task<IReadOnlyList<string>> GetIncompleteDependencyTitlesAsync(long taskId, CancellationToken cancellationToken)
        {
            return await context.TaskDependencies.AsNoTracking().Where(d => d.TaskId == taskId)
                .Select(d => d.DependsOnTask).Where(t => t.DeletedAtUtc == null && t.Status != TaskItemStatus.Done &&
                t.Status != TaskItemStatus.Cancelled).Select(t => t.Title).ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<(string Title, DateOnly DueDate)>> GetDependentsViolatingDueDateAsync(long taskId,
            DateOnly newDueDate, CancellationToken cancellationToken)
        {
            var rows = await context.TaskDependencies.AsNoTracking().Where(d => d.DependsOnTaskId == taskId).Select(d => d.Task)
                .Where(t => t.DeletedAtUtc == null && t.Status != TaskItemStatus.Cancelled && t.DueDate.HasValue && t.DueDate.Value < newDueDate)
                .Select(t => new { t.Title, t.DueDate }).ToListAsync(cancellationToken);

            return rows.Select(x => (x.Title, x.DueDate!.Value)).ToList();
        }

        public async Task<TaskItem?> GetByIdWithDependencyAsync(long taskId, long dependencyId, CancellationToken cancellationToken)
        {
            return await context.Tasks.Include(t => t.Dependencies.Where(d => d.Id == dependencyId))
                .FirstOrDefaultAsync(t => t.Id == taskId && t.DeletedAtUtc == null, cancellationToken);
        }

        public void RemoveDependency(TaskDependency dependency)
        {
            context.TaskDependencies.Remove(dependency);
        }

        public async Task DeleteAllDependenciesAsync(long taskId, CancellationToken cancellationToken)
        {
            await context.TaskDependencies.Where(d => d.TaskId == taskId || d.DependsOnTaskId == taskId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}