using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IPermissionRepository
    {
        Task<Permission?> GetByIdAsync(int permissionId, CancellationToken cancellationToken);
    }
}
