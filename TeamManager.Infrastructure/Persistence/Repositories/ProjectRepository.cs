using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class ProjectRepository(TeamManagerDbContext context) : IProjectRepository
    {
        public async Task AddAsync(Project newProject, CancellationToken cancellationToken)
        {
            await context.AddAsync(newProject, cancellationToken);
        }

        public Task<Project?> GetByNameAsync(Guid teamId, string name, CancellationToken cancellationToken)
        {
            return context.Projects.Where(p => p.Name == name && p.TeamId == teamId && p.DeletedAtUtc == null)
                .FirstOrDefaultAsync(cancellationToken);
        }

    }
}