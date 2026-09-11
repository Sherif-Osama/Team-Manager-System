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

        public async Task<Project?> GetByIdAsync(Guid projectID, CancellationToken cancellationToken)
        {
            var project = await context.Projects.FindAsync([projectID], cancellationToken);

            return project is null || project.DeletedAtUtc.HasValue ? null : project;
        }

        public async Task<Project?> GetByIdWithMembersAsync(Guid projectId, CancellationToken cancellationToken)
        {
            return await context.Projects.Include(p => p.Members.Where(m => m.Status == ProjectMemberStatus.Active))
                .FirstOrDefaultAsync(p => p.Id == projectId && p.DeletedAtUtc == null, cancellationToken);
        }

        public Task<Project?> GetByNameAsync(Guid teamId, string name, CancellationToken cancellationToken)
        {
            return context.Projects.Where(p => p.Name == name && p.TeamId == teamId && p.DeletedAtUtc == null)
                .FirstOrDefaultAsync(cancellationToken);
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
    }
}