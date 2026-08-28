using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class Team : Entity<Guid>
{
    private readonly List<TeamMember> _members = new();
    private readonly List<TeamInvitation> _invitations = new();
    private readonly List<Project> _projects = new();
    private readonly List<Label> _labels = new();

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public User Owner { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public User Creator { get; private set; } = null!;
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<TeamInvitation> Invitations => _invitations.AsReadOnly();
    public IReadOnlyCollection<Project> Projects => _projects.AsReadOnly();
    public IReadOnlyCollection<Label> Labels => _labels.AsReadOnly();

    private Team() { }

    public Team(Guid id, string name, Guid ownerUserId, Guid createdBy, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A team must have a name.");

        Id = id;
        Name = name;
        Description = description;
        OwnerUserId = ownerUserId;
        CreatedBy = createdBy;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A team must have a name.");

        Name = name;
        Touch();
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        Touch();
    }

    public void TransferOwnership(Guid newOwnerUserId)
    {
        if (OwnerUserId == newOwnerUserId)
            throw new DomainException("The specified user is already the team owner.");

        OwnerUserId = newOwnerUserId;
        Touch();
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("The team is already inactive.");

        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        if (DeletedAtUtc.HasValue)
            throw new DomainException("A deleted team cannot be activated.");

        if (IsActive)
            throw new DomainException("The team is already active.");

        IsActive = true;
        Touch();
    }

    public void SoftDelete()
    {
        if (DeletedAtUtc.HasValue)
            throw new DomainException("The team is already deleted.");

        DeletedAtUtc = DateTime.UtcNow;
        IsActive = false;
        Touch();
    }

    public TeamMember AddMember(Guid userId, TeamRole role, Guid? invitedBy = null)
    {
        if (_members.Any(m => m.UserId == userId && m.Status == TeamMemberStatus.Active))
            throw new DomainException("This user already has an active membership in the team.");

        var member = new TeamMember(Id, userId, role, invitedBy);
        _members.Add(member);
        return member;
    }

    public void RemoveMember(Guid userId, Guid removedBy)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId && m.Status == TeamMemberStatus.Active);
        if (member is null)
            throw new DomainException("This user does not have an active membership in the team.");

        member.Remove(removedBy);
    }

    public TeamInvitation Invite(Guid id, string invitedEmail, Guid invitedBy, TeamRole role,
        string tokenHash, DateTime expiresAtUtc)
    {
        if (_invitations.Any(i => i.InvitedEmail.Equals(invitedEmail, StringComparison.OrdinalIgnoreCase)
                                   && i.Status == TeamInvitationStatus.Pending))
            throw new DomainException("There is already a pending invitation for this email in the team.");

        var invitation = new TeamInvitation(id, Id, invitedEmail, invitedBy, role, tokenHash, expiresAtUtc);
        _invitations.Add(invitation);
        return invitation;
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
