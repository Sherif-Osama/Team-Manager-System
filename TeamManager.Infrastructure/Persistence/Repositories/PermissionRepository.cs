using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class PermissionRepository(TeamManagerDbContext context) : IPermissionRepository
    {
        public async Task<Permission?> GetByIdAsync(int permissionId, CancellationToken cancellationToken)
        {
            return await context.Permissions.FindAsync([permissionId], cancellationToken);
        }
    }
}
