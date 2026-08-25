using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;

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

    private ProjectMember()
    {
    }

    internal ProjectMember(Guid projectId, Guid userId, TeamRole teamRole, Guid? addedBy = null)
    {
        ProjectId = projectId;
        UserId = userId;
        TeamRole = teamRole;
        Status = ProjectMemberStatus.Active;
        AddedBy = addedBy;
        AddedAtUtc = DateTime.UtcNow;
    }

    public void ChangeRole(TeamRole role) => TeamRole = role;

    public void Remove()
    {
        if (Status == ProjectMemberStatus.Removed) return;

        Status = ProjectMemberStatus.Removed;
        RemovedAtUtc = DateTime.UtcNow;
    }
}
