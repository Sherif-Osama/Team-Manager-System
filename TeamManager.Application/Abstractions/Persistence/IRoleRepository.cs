using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IRoleRepository
    {
        Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken);
        Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken);
    }
}