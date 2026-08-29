using TeamManager.Application.Features.Teams.Team.Queries.GetTeam;
using TeamManager.Application.Features.Teams.Team.Queries.GetTeams;
using TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMember;
using TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMembers;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Abstractions.Persistence
{
    public interface ITeamRepository
    {
        Task AddAsync(Team team, CancellationToken cancellationToken);
        Task<GetMembersResponse> GetMembersAsync(Guid teamId, int page, int pageSize, CancellationToken cancellationToken);
        Task<GetTeamResponse?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken);
        Task<Team?> GetByIdForUpdateAsync(Guid teamId, CancellationToken cancellationToken);
        Task<Team?> GetByNameAsync(string name, CancellationToken cancellationToken);
        Task<GetTeamsResponse> GetPagedAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken);
        Task<Team?> GetByIdWithMembersAsync(Guid teamId, CancellationToken cancellationToken);
        Task<bool> ExistsAsync(Guid teamId, CancellationToken cancellationToken);
        Task<GetMemberResponse?> GetMemberAsync(Guid teamId, long memberId, CancellationToken cancellationToken);
        Task<TeamMember?> GetMemberForUpdateAsync(Guid teamId, long memberId, CancellationToken cancellationToken);
        Task<bool> HasActiveRoleAsync(Guid teamId, Guid userId, IReadOnlyCollection<TeamRole> roles, CancellationToken cancellationToken);
        Task<TeamMember?> GetMemberByUserIdAsync(Guid teamId, Guid userId, CancellationToken cancellationToken);
    }
}