using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class TeamMember : Entity<long>
{
    public Guid TeamId { get; private set; }
    public Team Team { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public TeamRole TeamRole { get; private set; }
    public TeamMemberStatus Status { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }
    public Guid? InvitedBy { get; private set; }
    public User? InvitedByUser { get; private set; }
    public DateTime? RemovedAtUtc { get; private set; }
    public Guid? RemovedBy { get; private set; }
    public User? RemovedByUser { get; private set; }

    private TeamMember()
    {
    }

    internal TeamMember(Guid teamId, Guid userId, TeamRole teamRole, Guid? invitedBy = null)
    {
        TeamId = teamId;
        UserId = userId;
        TeamRole = teamRole;
        Status = TeamMemberStatus.Active;
        InvitedBy = invitedBy;
        JoinedAtUtc = DateTime.UtcNow;
    }

    public void ChangeRole(TeamRole role)
    {
        if (Status != TeamMemberStatus.Active)
            throw new DomainException("Cannot change the role of a member who is not active.");

        TeamRole = role;
    }

    public void Remove(Guid removedBy)
    {
        if (Status == TeamMemberStatus.Removed) return;

        Status = TeamMemberStatus.Removed;
        RemovedAtUtc = DateTime.UtcNow;
        RemovedBy = removedBy;
    }
}
