using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository(TeamManagerDbContext context) : IUserRepository
    {

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return context.Users.AnyAsync(x => x.Email == email && x.DeletedAtUtc == null, cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            await context.Users.AddAsync(user, cancellationToken);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return context.Users.FirstOrDefaultAsync(x => x.Email == email &&
            x.DeletedAtUtc == null, cancellationToken);
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            await context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        }

        public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = await context.Users.FindAsync([userId], cancellationToken);

            return user is not null && user.DeletedAtUtc == null ? user : null;
        }

        public Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
        {
            return context.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, DateTime.UtcNow), cancellationToken);
        }

        public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken)
        {
            return context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.User.IsActive && ur.User.DeletedAtUtc == null
            && ur.Role.RolePermissions.Any(rp => rp.Permission.Code == permissionCode), cancellationToken);
        }

        public async Task<bool> IsLastSystemAdminAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.User.IsActive
            && ur.User.DeletedAtUtc == null && ur.Role.Name == "SystemAdmin" &&
            !context.UserRoles.Any(other => other.RoleId == ur.RoleId && other.UserId != userId && other.User.IsActive &&
            other.User.DeletedAtUtc == null), cancellationToken);
        }

        public Task<User?> GetByIdWithRolesAsync(Guid userId, CancellationToken cancellationToken)
        {
            return context.Users.Include(x => x.UserRoles)
                .FirstOrDefaultAsync(x => x.Id == userId && x.DeletedAtUtc == null, cancellationToken);
        }
    }
}
