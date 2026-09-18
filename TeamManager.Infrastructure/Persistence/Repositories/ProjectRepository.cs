using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class ProjectRepository(TeamManagerDbContext context) : IProjectRepository
    {
        public async Task AddAsync(Project newProject, CancellationToken cancellationToken)
        {
            await context.AddAsync(newProject, cancellationToken);
        }

        public Task RemoveActiveMembershipsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return context.ProjectMembers.Where(x => x.UserId == userId &&
                    (x.Status == ProjectMemberStatus.Active || x.Status == ProjectMemberStatus.Suspended)
                    && x.ProjectRole != ProjectRole.Owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ProjectMemberStatus.Removed)
                    .SetProperty(x => x.RemovedAtUtc, DateTime.UtcNow), cancellationToken);
        }

        public Task DeactivateOwnedProjectsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return context.Projects.Where(x => x.OwnerUserId == userId && x.Status == ProjectStatus.Active
            && x.DeletedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ProjectStatus.OnHold)
            .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
        }

        public async Task<Project?> GetByIdAsync(Guid projectID, CancellationToken cancellationToken)
        {
            var project = await context.Projects.FindAsync([projectID], cancellationToken);

            return project is null || project.DeletedAtUtc.HasValue ? null : project;
        }

        public async Task<Project?> GetByIdWithMembersAsync(Guid projectId, CancellationToken cancellationToken)
        {
            return await context.Projects.Include(p => p.Members.Where(m => m.Status == ProjectMemberStatus.Active ||
            m.Status == ProjectMemberStatus.Suspended))
                .FirstOrDefaultAsync(p => p.Id == projectId && p.DeletedAtUtc == null, cancellationToken);
        }

        public Task<Project?> GetByNameAsync(Guid teamId, string name, CancellationToken cancellationToken)
        {
            return context.Projects.Where(p => p.Name == name && p.TeamId == teamId && p.DeletedAtUtc == null)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<bool> HasActiveOwnedProjectAsync(Guid userId, CancellationToken cancellationToken)
        {
            return context.Projects.AnyAsync(p => p.OwnerUserId == userId && p.Status != ProjectStatus.Completed
            && p.DeletedAtUtc == null, cancellationToken);
        }

        public async Task<bool> HasActiveRoleAsync(Guid projectId, Guid userId, ProjectRole[] requiredRoles,
            CancellationToken cancellationToken)
        {
            return await context.ProjectMembers.AsNoTracking().AnyAsync(pm => pm.ProjectId == projectId
            &&
            pm.UserId == userId && pm.User.IsActive
            &&
            pm.Status == ProjectMemberStatus.Active && requiredRoles.Contains(pm.ProjectRole), cancellationToken);
        }

        public Task<bool> IsActiveMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
        {
            return context.ProjectMembers.AsNoTracking()
                .AnyAsync(pm => pm.ProjectId == projectId &&
                pm.UserId == userId && pm.User.IsActive && pm.Status == ProjectMemberStatus.Active, cancellationToken);
        }

        public Task SuspendActiveMembershipsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return context.ProjectMembers.Where(x => x.UserId == userId && x.Status == ProjectMemberStatus.Active
                && x.ProjectRole != ProjectRole.Owner)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ProjectMemberStatus.Suspended), cancellationToken);
        }

        public Task ReactivateSuspendedMembershipsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return context.ProjectMembers.Where(x => x.UserId == userId &&
            x.Status == ProjectMemberStatus.Suspended
            && x.ProjectRole != ProjectRole.Owner)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ProjectMemberStatus.Active), cancellationToken);
        }

        public Task RemoveMembershipsByTeamAsync(Guid teamId, Guid userId, CancellationToken cancellationToken)
        {
            return context.ProjectMembers.Where(pm => pm.Project.TeamId == teamId && pm.UserId == userId
                    && (pm.Status == ProjectMemberStatus.Active || pm.Status == ProjectMemberStatus.Suspended) && pm.ProjectRole
                    != ProjectRole.Owner).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ProjectMemberStatus.Removed)
                    .SetProperty(x => x.RemovedAtUtc, DateTime.UtcNow), cancellationToken);
        }
    }
}