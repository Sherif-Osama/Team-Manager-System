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
    public ProjectRole ProjectRole { get; private set; }
    public ProjectMemberStatus Status { get; private set; }
    public DateTime AddedAtUtc { get; private set; }
    public Guid? AddedBy { get; private set; }
    public User? AddedByUser { get; private set; }
    public DateTime? RemovedAtUtc { get; private set; }
    public Guid? RemovedBy { get; private set; }
    public User? RemovedByUser { get; private set; }

    private ProjectMember() { }

    internal ProjectMember(Guid projectId, Guid userId, ProjectRole projectRole, Guid? addedBy = null)
    {
        ProjectId = projectId;
        UserId = userId;
        ProjectRole = projectRole;
        Status = ProjectMemberStatus.Active;
        AddedBy = addedBy;
        AddedAtUtc = DateTime.UtcNow;
    }

    internal void ChangeRole(ProjectRole role)
    {
        if (Status == ProjectMemberStatus.Removed)
            throw new DomainException("Cannot change the role of a removed member.");

        if (role == ProjectRole)
            throw new DomainException($"Member already has {role} role");

        if (role == ProjectRole.Owner)
            throw new DomainException("Ownership cannot be changed");

        ProjectRole = role;
    }

    internal void PromoteToOwner()
    {
        if (Status != ProjectMemberStatus.Active)
            throw new DomainException("Only an active member can become the project owner.");

        ProjectRole = ProjectRole.Owner;
    }

    internal void Remove(Guid removedBy)
    {
        if (Status == ProjectMemberStatus.Removed)
            throw new DomainException("Member is already deleted");

        if (ProjectRole == ProjectRole.Owner)
            throw new DomainException("The project owner cannot be removed from the project.");

        Status = ProjectMemberStatus.Removed;
        RemovedBy = removedBy;
        RemovedAtUtc = DateTime.UtcNow;
    }
}