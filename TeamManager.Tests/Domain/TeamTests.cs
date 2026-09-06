using System.Reflection;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.Domain;

public sealed class TeamTests
{
    private static Team CreateTeam(Guid? ownerId = null)
    {
        var id = ownerId ?? Guid.NewGuid();
        return new Team(Guid.NewGuid(), "Test Team", id, id);
    }

    private void SetStatus(TeamMember member, TeamMemberStatus status)
    {
        var property = typeof(TeamMember).GetProperty(nameof(TeamMember.Status),
       BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
        property.SetValue(member, status);
    }

    private void SetExpiry(TeamInvitation invitation, DateTime expiresAtUtc)
    {
        var property = typeof(TeamInvitation).GetProperty(nameof(TeamInvitation.ExpiresAtUtc),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        property.SetValue(invitation, expiresAtUtc);
    }

    private void SetStatus(TeamInvitation invitation, TeamInvitationStatus status)
    {
        var property = typeof(TeamInvitation).GetProperty(nameof(TeamInvitation.Status),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        property.SetValue(invitation, status);
    }

    [Fact]
    public void AddMember_ToActiveTeam_CreatesMembershipWithExpectedData()
    {
        var team = CreateTeam();
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var member = team.AddMember(userId, TeamRole.Member);

        Assert.Equal(team.Id, member.TeamId);
        Assert.Equal(userId, member.UserId);
        Assert.Equal(TeamRole.Member, member.TeamRole);
        Assert.Equal(TeamMemberStatus.Active, member.Status);
        Assert.InRange(member.JoinedAtUtc, before, DateTime.UtcNow);
    }

    [Fact]
    public void AddMember_ToInactiveTeam_Throws()
    {
        var team = CreateTeam();
        team.Deactivate();

        Assert.Throws<DomainException>(() => team.AddMember(Guid.NewGuid(), TeamRole.Member));
    }

    [Fact]
    public void AddMember_WithOwnerRole_Throws()
    {
        var team = CreateTeam();
        Assert.Throws<DomainException>(() => team.AddMember(Guid.NewGuid(), TeamRole.Owner));
    }

    [Fact]
    public void AddMember_WhenUserHasActiveMembership_Throws()
    {
        var team = CreateTeam();
        var userId = Guid.NewGuid();
        team.AddMember(userId, TeamRole.Member);

        Assert.Throws<DomainException>(() => team.AddMember(userId, TeamRole.Viewer));
    }

    [Fact]
    public void AddMember_WhenUserHasSuspendedMembership_Throws()
    {
        var team = CreateTeam();
        var member = team.AddMember(Guid.NewGuid(), TeamRole.Member);
        SetStatus(member, TeamMemberStatus.Suspended);

        Assert.Throws<DomainException>(() => team.AddMember(member.UserId, TeamRole.Viewer));
    }

    [Fact]
    public void AddMember_WhenUserHasRemovedMembership_CreatesNewActiveMembership()
    {
        var team = CreateTeam();
        var member = team.AddMember(Guid.NewGuid(), TeamRole.Member);
        SetStatus(member, TeamMemberStatus.Removed);

        var newMember = team.AddMember(member.UserId, TeamRole.Viewer);

        Assert.Equal(TeamMemberStatus.Active, newMember.Status);
        Assert.Equal(TeamRole.Viewer, newMember.TeamRole);
        Assert.Equal(2, team.Members.Count(m => m.UserId == member.UserId));
    }

    [Fact]
    public void Invite_NewUser_CreatesPendingInvitation()
    {
        var team = CreateTeam();
        var invitedBy = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var before = DateTime.UtcNow;

        var invitation = team.Invite("invitee@example.com", Guid.NewGuid(), invitedBy,
            TeamRole.Member, "token-hash", expiresAt);

        Assert.Equal(team.Id, invitation.TeamId);
        Assert.Equal("invitee@example.com", invitation.InvitedEmail);
        Assert.Equal(invitedBy, invitation.InvitedBy);
        Assert.Equal(TeamRole.Member, invitation.TeamRole);
        Assert.Equal("token-hash", invitation.TokenHash);
        Assert.Equal(TeamInvitationStatus.Pending, invitation.Status);
        Assert.Equal(expiresAt, invitation.ExpiresAtUtc);
        Assert.InRange(invitation.CreatedAtUtc, before, DateTime.UtcNow);
    }

    [Fact]
    public void Invite_UserWithActiveMembership_Throws()
    {
        var team = CreateTeam();
        var member = team.AddMember(Guid.NewGuid(), TeamRole.Member);

        Assert.Throws<DomainException>(() => team.Invite("invitee@example.com", member.UserId,
            Guid.NewGuid(), TeamRole.Member, "token-hash", DateTime.UtcNow.AddDays(7)));
    }

    [Fact]
    public void Invite_UserWithSuspendedMembership_Throws()
    {
        var team = CreateTeam();
        var member = team.AddMember(Guid.NewGuid(), TeamRole.Member);
        SetStatus(member, TeamMemberStatus.Suspended);

        Assert.Throws<DomainException>(() => team.Invite("invitee@example.com", member.UserId,
            Guid.NewGuid(), TeamRole.Member, "token-hash", DateTime.UtcNow.AddDays(7)));
    }

    [Fact]
    public void Invite_UserWithRemovedMembership_Succeeds()
    {
        var team = CreateTeam();
        var member = team.AddMember(Guid.NewGuid(), TeamRole.Member);
        SetStatus(member, TeamMemberStatus.Removed);

        var invitation = team.Invite("invitee@example.com", member.UserId, Guid.NewGuid(),
            TeamRole.Member, "token-hash", DateTime.UtcNow.AddDays(7));

        Assert.Equal(TeamInvitationStatus.Pending, invitation.Status);
        Assert.Equal(member.UserId, invitation.InvitedUserId);
        Assert.Equal(invitation.TeamId, team.Id);
    }

    [Fact]
    public void Invite_WithOwnerRole_Throws()
    {
        var team = CreateTeam();

        Assert.Throws<DomainException>(() => team.Invite("invitee@example.com", null,
            Guid.NewGuid(), TeamRole.Owner, "token-hash", DateTime.UtcNow.AddDays(7)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Invite_WithBlankEmail_Throws(string? email)
    {
        var team = CreateTeam();

        Assert.Throws<DomainException>(() => team.Invite(email!, null, Guid.NewGuid(),
            TeamRole.Member, "token-hash", DateTime.UtcNow.AddDays(7)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Invite_WithBlankToken_Throws(string? token)
    {
        var team = CreateTeam();

        Assert.Throws<DomainException>(() => team.Invite("invitee@example.com", null,
            Guid.NewGuid(), TeamRole.Member, token!, DateTime.UtcNow.AddDays(7)));
    }

    [Fact]
    public void Invite_WithExpiredDate_Throws()
    {
        var team = CreateTeam();

        Assert.Throws<DomainException>(() => team.Invite("invitee@example.com", null,
            Guid.NewGuid(), TeamRole.Member, "token-hash", DateTime.UtcNow.AddSeconds(-1)));
    }

    [Fact]
    public void Invite_WithExistingPendingEmail_Throws()
    {
        var team = CreateTeam();
        team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash-1", DateTime.UtcNow.AddDays(7));

        Assert.Throws<DomainException>(() => team.Invite("INVITEE@example.com", null,
            Guid.NewGuid(), TeamRole.Admin, "token-hash-2", DateTime.UtcNow.AddDays(7)));
    }

    [Fact]
    public void Invite_ToInactiveTeam_Throws()
    {
        var team = CreateTeam();
        team.Deactivate();

        Assert.Throws<DomainException>(() => team.Invite("invitee@example.com", null,
            Guid.NewGuid(), TeamRole.Member, "token-hash", DateTime.UtcNow.AddDays(7)));
    }

    [Fact]
    public void AcceptInvitation_WithValidInvitation_CreatesActiveMember()
    {
        var team = CreateTeam();
        var userId = Guid.NewGuid();
        var invitation = team.Invite("invitee@example.com", userId, Guid.NewGuid(),
            TeamRole.Member, "token-hash", DateTime.UtcNow.AddDays(7));

        int memberCount = team.Members.Count;

        var member = team.AcceptInvitation(invitation.TokenHash, userId, "invitee@example.com");

        Assert.Equal(userId, member.UserId);
        Assert.Equal(TeamRole.Member, member.TeamRole);
        Assert.Equal(TeamMemberStatus.Active, member.Status);
        Assert.Equal(TeamInvitationStatus.Accepted, invitation.Status);
        Assert.Equal(userId, invitation.InvitedUserId);
        Assert.NotNull(invitation.AcceptedAtUtc);
        Assert.Equal(memberCount + 1, team.Members.Count());
    }

    [Fact]
    public void AcceptInvitation_WithUnknownToken_Throws()
    {
        var team = CreateTeam();

        Assert.Throws<DomainException>(() =>
            team.AcceptInvitation("unknown-token", Guid.NewGuid(), "invitee@example.com"));
    }

    [Theory]
    [InlineData(TeamInvitationStatus.Expired)]
    [InlineData(TeamInvitationStatus.Cancelled)]
    [InlineData(TeamInvitationStatus.Rejected)]
    [InlineData(TeamInvitationStatus.Accepted)]
    public void AcceptInvitation_WhenInvitationIsNotPending_Throws(TeamInvitationStatus status)
    {
        var team = CreateTeam();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member, "token-hash",
            DateTime.UtcNow.AddDays(7));

        SetStatus(invitation, status);

        Assert.Throws<DomainException>(() => team.AcceptInvitation(invitation.TokenHash, Guid.NewGuid(), "invitee@example.com"));
    }

    [Fact]
    public void AcceptInvitation_WhenExpired_MarksInvitationExpiredAndThrows()
    {
        var team = CreateTeam();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));

        SetExpiry(invitation, DateTime.UtcNow.AddSeconds(-1));

        Assert.Throws<DomainException>(() => team.AcceptInvitation(invitation.TokenHash, Guid.NewGuid(), "invitee@example.com"));

        Assert.Equal(TeamInvitationStatus.Expired, invitation.Status);
    }

    [Fact]
    public void AcceptInvitation_WithDifferentEmail_Throws()
    {
        var team = CreateTeam();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));

        Assert.Throws<DomainException>(() => team.AcceptInvitation(invitation.TokenHash, Guid.NewGuid(), "other@example.com"));
    }

    [Fact]
    public void AcceptInvitation_WhenBoundToAnotherUser_Throws()
    {
        var team = CreateTeam();
        var invitedUserId = Guid.NewGuid();
        var invitation = team.Invite("invitee@example.com", invitedUserId, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));

        Assert.Throws<DomainException>(() => team.AcceptInvitation(invitation.TokenHash, Guid.NewGuid(), "invitee@example.com"));
    }

    [Fact]
    public void AcceptInvitation_ToInactiveTeam_Throws()
    {
        var team = CreateTeam();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));
        team.Deactivate();

        Assert.Throws<DomainException>(() => team.AcceptInvitation(invitation.TokenHash, Guid.NewGuid(), "invitee@example.com"));
    }

    [Fact]
    public void AcceptInvitation_WhenUserIsAlreadyActiveMember_AcceptsWithoutDuplicate()
    {
        var team = CreateTeam();
        var userId = Guid.NewGuid();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));
        var existingMember = team.AddMember(userId, TeamRole.Admin);

        var returnedMember = team.AcceptInvitation(invitation.TokenHash, userId, "invitee@example.com");

        Assert.Same(existingMember, returnedMember);
        Assert.Single(team.Members, member => member.UserId == userId);
        Assert.Equal(TeamInvitationStatus.Accepted, invitation.Status);
    }

    [Fact]
    public void AcceptInvitation_WhenUserHasSuspendedMembership_AcceptsWithoutDuplicate()
    {
        var team = CreateTeam();
        var userId = Guid.NewGuid();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));
        var existingMember = team.AddMember(userId, TeamRole.Admin);
        SetStatus(existingMember, TeamMemberStatus.Suspended);

        var returnedMember = team.AcceptInvitation(invitation.TokenHash, userId, "invitee@example.com");

        Assert.Same(existingMember, returnedMember);
        Assert.Equal(TeamMemberStatus.Suspended, returnedMember.Status);
        Assert.Single(team.Members, member => member.UserId == userId);
        Assert.Equal(TeamInvitationStatus.Accepted, invitation.Status);
    }

    [Fact]
    public void RejectInvitation_WithValidInvitation_RejectsAndRecordsTimestamp()
    {
        var team = CreateTeam();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));

        team.RejectInvitation(invitation.TokenHash, Guid.NewGuid(), "invitee@example.com");

        Assert.Equal(TeamInvitationStatus.Rejected, invitation.Status);
        Assert.NotNull(invitation.RejectedAtUtc);
    }

    [Fact]
    public void RejectInvitation_WhenExpired_ThrowsAndRemainsPending()
    {
        var team = CreateTeam();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));
        SetExpiry(invitation, DateTime.UtcNow.AddSeconds(-1));

        Assert.Throws<DomainException>(() =>
            team.RejectInvitation(invitation.TokenHash, Guid.NewGuid(), "invitee@example.com"));

        Assert.Equal(TeamInvitationStatus.Pending, invitation.Status);
    }

    [Fact]
    public void RejectInvitation_WhenNotPending_Throws()
    {
        var team = CreateTeam();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));
        team.RejectInvitation(invitation.TokenHash, Guid.NewGuid(), "invitee@example.com");

        Assert.Throws<DomainException>(() =>
            team.RejectInvitation(invitation.TokenHash, Guid.NewGuid(), "invitee@example.com"));
    }

    [Fact]
    public void RejectInvitation_WithDifferentEmail_Throws()
    {
        var team = CreateTeam();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));

        Assert.Throws<DomainException>(() =>
            team.RejectInvitation(invitation.TokenHash, Guid.NewGuid(), "other@example.com"));
    }

    [Fact]
    public void RejectInvitation_WhenBoundToAnotherUser_Throws()
    {
        var team = CreateTeam();
        var invitedUserId = Guid.NewGuid();
        var invitation = team.Invite("invitee@example.com", invitedUserId, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));

        Assert.Throws<DomainException>(() =>
            team.RejectInvitation(invitation.TokenHash, Guid.NewGuid(), "invitee@example.com"));
    }

    [Fact]
    public void CancelInvitation_WhenPending_CancelsAndRecordsTimestamp()
    {
        var team = CreateTeam();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));

        team.CancelInvitation(invitation.Id);

        Assert.Equal(TeamInvitationStatus.Cancelled, invitation.Status);
        Assert.NotNull(invitation.CancelledAtUtc);
    }

    [Fact]
    public void CancelInvitation_WhenNotFound_Throws()
    {
        var team = CreateTeam();

        Assert.Throws<DomainException>(() => team.CancelInvitation(Guid.NewGuid()));
    }

    [Fact]
    public void CancelInvitation_WhenNotPending_Throws()
    {
        var team = CreateTeam();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));
        team.CancelInvitation(invitation.Id);

        Assert.Throws<DomainException>(() => team.CancelInvitation(invitation.Id));
    }

    [Fact]
    public void CancelInvitation_WhenExpired_Throws()
    {
        var team = CreateTeam();
        var invitation = team.Invite("invitee@example.com", null, Guid.NewGuid(), TeamRole.Member,
            "token-hash", DateTime.UtcNow.AddDays(7));

        SetExpiry(invitation, DateTime.UtcNow.AddSeconds(-1));

        Assert.Throws<DomainException>(() => team.CancelInvitation(invitation.Id));
    }
}