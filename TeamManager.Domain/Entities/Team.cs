using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities
{

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

            var ownerMembership = new TeamMember(Id, ownerUserId, TeamRole.Owner);

            _members.Add(ownerMembership);
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

            var currentOwner = _members.FirstOrDefault(m => m.UserId == OwnerUserId && m.Status == TeamMemberStatus.Active);

            if (currentOwner is null)
                throw new DomainException("The current team owner must have an active membership.");

            var newOwner = _members.FirstOrDefault(m => m.UserId == newOwnerUserId && m.Status == TeamMemberStatus.Active);

            if (newOwner is null)
                throw new DomainException("The new owner must be an active team member.");

            currentOwner.ChangeRole(TeamRole.Admin);
            newOwner.PromoteToOwner();
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
            if (!IsActive)
                throw new DomainException("Cannot add a member to an inactive team.");

            if (role == TeamRole.Owner)
                throw new DomainException("Ownership cannot be assigned via AddMember. Use TransferOwnership instead.");

            if (_members.Any(m => m.UserId == userId && m.Status == TeamMemberStatus.Active))
                throw new DomainException("This user already has an active membership in the team.");

            var member = new TeamMember(Id, userId, role, invitedBy);

            _members.Add(member);

            return member;
        }

        public void RemoveMember(long memberId, Guid removedBy)
        {
            var member = _members.FirstOrDefault(m => m.Id == memberId &&
                (m.Status == TeamMemberStatus.Active || m.Status == TeamMemberStatus.Suspended));

            if (member is null)
                throw new DomainException("This user does not have a removable membership in the team.");

            if (member.TeamRole == TeamRole.Owner)
                throw new DomainException("The team owner cannot be removed from the team.");

            member.Remove(removedBy);
        }

        public void ChangeMemberRole(long memberId, TeamRole role)
        {
            var member = _members.FirstOrDefault(m => m.Id == memberId &&
                (m.Status == TeamMemberStatus.Active || m.Status == TeamMemberStatus.Suspended));

            if (member is null)
                throw new DomainException("This member does not have a manageable membership in the team.");

            if (member.TeamRole == TeamRole.Owner)
                throw new DomainException("Ownership cannot be changed");

            member.ChangeRole(role);
        }

        public TeamInvitation Invite(string invitedEmail, Guid? invitedUserId, Guid invitedBy, TeamRole role,
            string tokenHash, DateTime expiresAtUtc)
        {
            if (!IsActive)
                throw new DomainException("Cannot invite members to an inactive team.");

            if (role == TeamRole.Owner)
                throw new DomainException("Ownership cannot be assigned via invitation. Use TransferOwnership instead.");

            if (invitedUserId.HasValue && _members.Any(m => m.UserId == invitedUserId.Value &&
                (m.Status == TeamMemberStatus.Active || m.Status == TeamMemberStatus.Suspended)))
                throw new DomainException("This user is already a member of the team.");


            if (_invitations.Any(i => i.InvitedEmail.Equals(invitedEmail, StringComparison.OrdinalIgnoreCase)
                                       && i.Status == TeamInvitationStatus.Pending))
                throw new DomainException("There is already a pending invitation for this email in the team.");

            var invitation = new TeamInvitation(Id, invitedEmail, invitedUserId, invitedBy, role, tokenHash, expiresAtUtc);

            _invitations.Add(invitation);

            return invitation;
        }

        public TeamMember AcceptInvitation(string tokenHash, Guid userId, string userEmail)
        {
            if (!IsActive)
                throw new DomainException("Cannot accept an invitation for an inactive team.");

            var invitation = _invitations.FirstOrDefault(i => i.TokenHash == tokenHash);

            if (invitation is null)
                throw new DomainException("Invitation not found.");


            if (invitation.Status != TeamInvitationStatus.Pending)
                throw new DomainException($"This invitation is already {invitation.Status}.");

            if (invitation.ExpiresAtUtc <= DateTime.UtcNow)
            {
                invitation.MarkExpired();
                throw new DomainException("This invitation has expired.");
            }

            if (!string.Equals(invitation.InvitedEmail, userEmail, StringComparison.OrdinalIgnoreCase))
                throw new DomainException("This invitation belongs to another user.");


            if (invitation.InvitedUserId.HasValue && invitation.InvitedUserId != userId)
                throw new DomainException("This invitation belongs to another user.");

            var existingMember = _members.FirstOrDefault(m => m.UserId == userId &&
                (m.Status == TeamMemberStatus.Active || m.Status == TeamMemberStatus.Suspended));

            if (existingMember is not null)
            {
                invitation.Accept(userId);
                return existingMember;
            }

            var newMember = AddMember(userId, invitation.TeamRole, invitation.InvitedBy);

            invitation.Accept(userId);

            return newMember;
        }

        public void RejectInvitation(string tokenHash, Guid userId, string userEmail)
        {
            var invitation = _invitations.FirstOrDefault(i => i.TokenHash == tokenHash);

            if (invitation is null)
                throw new DomainException("Invitation not found.");

            if (!string.Equals(invitation.InvitedEmail, userEmail, StringComparison.OrdinalIgnoreCase))
                throw new DomainException("This invitation belongs to another user.");

            if (invitation.InvitedUserId.HasValue && invitation.InvitedUserId.Value != userId)
                throw new DomainException("This invitation belongs to another user.");

            invitation.Reject();
        }

        public void CancelInvitation(Guid invitationId)
        {
            var invitation = _invitations.FirstOrDefault(i => i.Id == invitationId);

            if (invitation is null)
                throw new DomainException("Invitation not found.");

            invitation.Cancel();
        }

        private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
    }
}
