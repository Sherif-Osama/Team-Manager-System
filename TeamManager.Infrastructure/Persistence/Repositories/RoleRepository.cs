using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class RoleRepository(TeamManagerDbContext context) : IRoleRepository
    {
        public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
        {
            return context.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        }
        public Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken)
        {
            return context.Roles.FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);
        }
    }
}