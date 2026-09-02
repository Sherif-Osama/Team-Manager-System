using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository(TeamManagerDbContext context) : IUserRepository
    {

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return context.Users.AnyAsync(x => x.Email == email, cancellationToken);
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

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.DeletedAtUtc == null, cancellationToken);
        }

        public Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
        {
            return context.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, DateTime.UtcNow), cancellationToken);
        }
    }
}