using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface ITeamRepository
    {
        Task AddAsync(Team team, CancellationToken cancellationToken);
        Task<Team?> GetByIdForUpdateAsync(Guid teamId, CancellationToken cancellationToken);
        Task<Team?> GetByNameAsync(string name, CancellationToken cancellationToken);
        Task<Team?> GetByIdWithMembersAsync(Guid teamId, CancellationToken cancellationToken);
        Task<bool> HasActiveRoleAsync(Guid teamId, Guid userId, IReadOnlyCollection<TeamRole> roles, CancellationToken cancellationToken);
        Task<Team?> GetByIdWithMembersAndInvitationsAsync(Guid teamId, CancellationToken cancellationToken);
        Task<Team?> GetByInvitationTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
        Task<Team?> GetByIdWithInvitationsAsync(Guid teamId, CancellationToken cancellationToken);
        Task LinkPendingInvitationsToUserAsync(string email, Guid userId, CancellationToken cancellationToken);
    }
}