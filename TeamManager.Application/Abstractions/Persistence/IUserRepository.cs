using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IUserRepository
    {
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
        Task AddAsync(User user, CancellationToken cancellationToken);
    }
}