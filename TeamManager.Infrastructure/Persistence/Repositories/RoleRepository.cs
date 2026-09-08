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

        public async Task<bool> ExistsAdminAsync(CancellationToken cancellationToken)
        {
            var roleIdquery = context.Roles.Where(r => r.Name == "SystemAdmin").Select(r => r.Id);

            return await context.UserRoles.AnyAsync(ur => ur.RoleId == roleIdquery.FirstOrDefault(), cancellationToken);
        }
    }
}