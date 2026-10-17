using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IRoleRepository
    {
        Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken);
        Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken);
        Task<Role?> GetByIdWithPermissionsAsync(int roleId, CancellationToken cancellationToken);
        Task<bool> ExistsAdminAsync(int roleId, CancellationToken cancellationToken);
        Task<bool> IsAssignedToUsersAsync(int roleId, CancellationToken cancellationToken);
        Task AddAsync(Role role, CancellationToken cancellationToken);
        void Delete(Role role);
    }
}