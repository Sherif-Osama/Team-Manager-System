using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions
{
    public interface IProjectRepository
    {
        Task<Project?> GetByNameAsync(Guid teamID, string name, CancellationToken cancellationToken);
        Task AddAsync(Project newProject, CancellationToken cancellation);
    }
}