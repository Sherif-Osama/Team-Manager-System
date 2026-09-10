using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class ProjectMember : Entity<long>
{
    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public TeamRole TeamRole { get; private set; }
    public ProjectMemberStatus Status { get; private set; }
    public DateTime AddedAtUtc { get; private set; }
    public Guid? AddedBy { get; private set; }
    public User? AddedByUser { get; private set; }
    public DateTime? RemovedAtUtc { get; private set; }

    private ProjectMember() { }

    internal ProjectMember(Guid projectId, Guid userId, TeamRole teamRole, Guid? addedBy = null)
    {
        ProjectId = projectId;
        UserId = userId;
        TeamRole = teamRole;
        Status = ProjectMemberStatus.Active;
        AddedBy = addedBy;
        AddedAtUtc = DateTime.UtcNow;
    }

    internal void ChangeRole(TeamRole role)
    {
        if (role == TeamRole)
            throw new DomainException($"Member already has {role} role");

        if (role == TeamRole.Owner)
            throw new DomainException("Ownership cannot be changed, Use TransferOwnership instead.");

        TeamRole = role;
    }


    internal void Remove()
    {
        if (Status == ProjectMemberStatus.Removed)
            throw new DomainException("Member is already deleted");

        if (TeamRole == TeamRole.Owner)
            throw new DomainException("The project owner cannot be removed from the project.");

        Status = ProjectMemberStatus.Removed;
        RemovedAtUtc = DateTime.UtcNow;
    }

    internal void PromoteToOwner()
    {
        if (Status != ProjectMemberStatus.Active)
            throw new DomainException("Cannot set inactive as project owner");

        if (TeamRole == TeamRole.Owner)
            throw new DomainException("Member is already project owner");

        TeamRole = TeamRole.Owner;
    }
}