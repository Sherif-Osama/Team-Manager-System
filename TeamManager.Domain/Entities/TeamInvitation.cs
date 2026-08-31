using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class TeamInvitation : Entity<Guid>
{
    public Guid TeamId { get; private set; }
    public Team Team { get; private set; } = null!;
    public string InvitedEmail { get; private set; } = null!;
    public Guid? InvitedUserId { get; private set; }
    public User? InvitedUser { get; private set; }
    public Guid InvitedBy { get; private set; }
    public User InvitedByUser { get; private set; } = null!;
    public TeamRole TeamRole { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public TeamInvitationStatus Status { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private TeamInvitation() { }

    internal TeamInvitation(Guid teamId, string invitedEmail, Guid? invitedUserId, Guid invitedBy, TeamRole teamRole,
        string tokenHash, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(invitedEmail))
            throw new DomainException("An invitation must target an email address.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("An invitation must have a token hash.");
        if (expiresAtUtc <= DateTime.UtcNow)
            throw new DomainException("An invitation cannot be created already expired.");
        if (teamRole == TeamRole.Owner)
            throw new DomainException("A team invitation cannot assign the owner role.");

        TeamId = teamId;
        InvitedEmail = invitedEmail;
        InvitedUserId = invitedUserId;
        InvitedBy = invitedBy;
        TeamRole = teamRole;
        TokenHash = tokenHash;
        Status = TeamInvitationStatus.Pending;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    internal void Accept(Guid userId)
    {
        EnsurePending();
        InvitedUserId = userId;
        Status = TeamInvitationStatus.Accepted;
        AcceptedAtUtc = DateTime.UtcNow;
    }

    internal void Reject()
    {
        EnsurePending();
        Status = TeamInvitationStatus.Rejected;
        RejectedAtUtc = DateTime.UtcNow;
    }

    internal void Cancel()
    {
        EnsurePending();
        Status = TeamInvitationStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        if (Status != TeamInvitationStatus.Pending) return;
        Status = TeamInvitationStatus.Expired;
    }

    private void EnsurePending()
    {
        if (Status != TeamInvitationStatus.Pending)
            throw new DomainException($"Cannot transition an invitation that is already {Status}.");
    }
}
