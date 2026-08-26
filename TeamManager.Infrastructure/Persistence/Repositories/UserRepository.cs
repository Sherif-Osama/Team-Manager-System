using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;

namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly TeamManagerDbContext _context;

        public UserRepository(TeamManagerDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return _context.Users.AnyAsync(x => x.Email == email, cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            await _context.Users.AddAsync(user, cancellationToken);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
