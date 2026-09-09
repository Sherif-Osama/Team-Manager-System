using System.Reflection;
using TeamManager.Domain.Common;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.UnitTest.Domain
{
    public sealed class TeamMemberTests
    {
        //Helpers
        private (Team Team, TeamMember Member) CreateTeamWithMember()
        {
            var ownerId = Guid.NewGuid();
            var team = new Team(Guid.NewGuid(), "Team", ownerId, ownerId);
            var member = team.AddMember(Guid.NewGuid(), TeamRole.Member);

            SetId(member, 1);
            return (team, member);
        }

        private void SetStatus(TeamMember member, TeamMemberStatus status)
        {
            var property = typeof(TeamMember).GetProperty(nameof(TeamMember.Status),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            property.SetValue(member, status);
        }

        private void SetId(TeamMember member, long id)
        {
            var property = typeof(Entity<long>).GetProperty(nameof(Entity<long>.Id),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            property.SetValue(member, id);
        }

        [Fact]
        public void ChangeMemberRole_WhenActive_ChangesRoleAndKeepsStatus()
        {
            var (team, member) = CreateTeamWithMember();

            team.ChangeMemberRole(member.Id, TeamRole.Admin);

            Assert.Equal(TeamRole.Admin, member.TeamRole);
            Assert.Equal(TeamMemberStatus.Active, member.Status);
        }

        [Fact]
        public void ChangeMemberRole_WhenSuspended_ChangesRoleAndKeepsSuspendedStatus()
        {
            var (team, member) = CreateTeamWithMember();
            SetStatus(member, TeamMemberStatus.Suspended);

            team.ChangeMemberRole(member.Id, TeamRole.Viewer);

            Assert.Equal(TeamRole.Viewer, member.TeamRole);
            Assert.Equal(TeamMemberStatus.Suspended, member.Status);
        }

        [Fact]
        public void ChangeMemberRole_WhenRemoved_Throws()
        {
            var (team, member) = CreateTeamWithMember();
            SetStatus(member, TeamMemberStatus.Removed);

            Assert.Throws<DomainException>(() => team.ChangeMemberRole(member.Id, TeamRole.Admin));
        }

        [Fact]
        public void ChangeMemberRole_ToOwner_Throws()
        {
            var (team, member) = CreateTeamWithMember();

            Assert.Throws<DomainException>(() => team.ChangeMemberRole(member.Id, TeamRole.Owner));
        }

        [Fact]
        public void ChangeMemberRole_ToSameRole_Throws()
        {
            var (team, member) = CreateTeamWithMember();

            Assert.Throws<DomainException>(() => team.ChangeMemberRole(member.Id, TeamRole.Member));
        }

        [Fact]
        public void ChangeMemberRole_WithUnknownMember_Throws()
        {
            var (team, _) = CreateTeamWithMember();

            Assert.Throws<DomainException>(() => team.ChangeMemberRole(999, TeamRole.Admin));
        }

        [Fact]
        public void RemoveMember_WhenActive_SetsRemovedMetadata()
        {
            var (team, member) = CreateTeamWithMember();
            var removedBy = Guid.NewGuid();

            team.RemoveMember(member.Id, removedBy);

            Assert.Equal(TeamMemberStatus.Removed, member.Status);
            Assert.NotNull(member.RemovedAtUtc);
            Assert.Equal(removedBy, member.RemovedBy);
        }

        [Fact]
        public void RemoveMember_WhenSuspended_SetsRemovedMetadata()
        {
            var (team, member) = CreateTeamWithMember();
            SetStatus(member, TeamMemberStatus.Suspended);
            var removedBy = Guid.NewGuid();

            team.RemoveMember(member.Id, removedBy);

            Assert.Equal(TeamMemberStatus.Removed, member.Status);
            Assert.NotNull(member.RemovedAtUtc);
            Assert.Equal(removedBy, member.RemovedBy);
        }

        [Fact]
        public void RemoveMember_WhenRemoved_Throws()
        {
            var (team, member) = CreateTeamWithMember();
            SetStatus(member, TeamMemberStatus.Removed);

            Assert.Throws<DomainException>(() => team.RemoveMember(member.Id, Guid.NewGuid()));
        }

        [Fact]
        public void RemoveMember_WhenOwner_Throws()
        {
            var (team, member) = CreateTeamWithMember();

            var Property = typeof(TeamMember).GetProperty(
                nameof(TeamMember.TeamRole),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

            Property.SetValue(member, TeamRole.Owner);

            Assert.Throws<DomainException>(() => team.RemoveMember(member.Id, Guid.NewGuid()));
        }

        [Fact]
        public void RemoveMember_WithUnknownMember_Throws()
        {
            var (team, _) = CreateTeamWithMember();

            Assert.Throws<DomainException>(() => team.RemoveMember(999, Guid.NewGuid()));
        }

        [Fact]
        public void TransferOwnership_ToActiveMember_TransfersRolesAndOwnerId()
        {
            var (team, member) = CreateTeamWithMember();

            team.TransferOwnership(member.UserId);

            Assert.Equal(member.UserId, team.OwnerUserId);
            Assert.Equal(TeamRole.Admin, team.Members.Single(m => m.UserId != member.UserId).TeamRole);
            Assert.Equal(TeamRole.Owner, member.TeamRole);
            Assert.Equal(TeamMemberStatus.Active, member.Status);
        }

        [Fact]
        public void TransferOwnership_ToSuspendedMember_Throws()
        {
            var (team, member) = CreateTeamWithMember();
            SetStatus(member, TeamMemberStatus.Suspended);

            Assert.Throws<DomainException>(() => team.TransferOwnership(member.UserId));
        }

        [Fact]
        public void TransferOwnership_ToRemovedMember_Throws()
        {
            var (team, member) = CreateTeamWithMember();
            SetStatus(member, TeamMemberStatus.Removed);

            Assert.Throws<DomainException>(() => team.TransferOwnership(member.UserId));
        }

        [Fact]
        public void TransferOwnership_ToUnknownUser_Throws()
        {
            var (team, _) = CreateTeamWithMember();

            Assert.Throws<DomainException>(() => team.TransferOwnership(Guid.NewGuid()));
        }

        [Fact]
        public void TransferOwnership_ToCurrentOwner_Throws()
        {
            var ownerId = Guid.NewGuid();
            var team = new Team(Guid.NewGuid(), "Team", ownerId, ownerId);

            Assert.Throws<DomainException>(() => team.TransferOwnership(ownerId));
        }
    }
}