using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Abstractions
{
    public interface IProjectRepository
    {
        Task AddAsync(Project newProject, CancellationToken cancellation);
        Task<Project?> GetByIdAsync(Guid projectID, CancellationToken cancellationToken);
        Task<Project?> GetByNameAsync(Guid teamID, string name, CancellationToken cancellationToken);
        Task<bool> HasActiveRoleAsync(Guid projectId, Guid userId, ProjectRole[] requiredRoles, CancellationToken cancellationToken);
        Task<Project?> GetByIdWithMembersAsync(Guid projectId, CancellationToken cancellationToken);
        Task DeactivateOwnedProjectsAsync(Guid userId, CancellationToken cancellationToken);
        Task<bool> HasActiveOwnedProjectAsync(Guid userId, CancellationToken cancellationToken);
        Task RemoveActiveMembershipsAsync(Guid userId, CancellationToken cancellationToken);
        Task SuspendActiveMembershipsAsync(Guid userId, CancellationToken cancellationToken);
        Task ReactivateSuspendedMembershipsAsync(Guid userId, CancellationToken cancellationToken);
        Task RemoveMembershipsByTeamAsync(Guid teamId, Guid userId, CancellationToken cancellationToken);
    }
}