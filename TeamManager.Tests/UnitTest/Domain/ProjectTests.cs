using System.Reflection;
using TeamManager.Domain.Common;
using TeamManager.Domain.Common.Events.Projects;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.UnitTest.Domain
{
    /// <summary>
    /// Tests Project aggregate behavior: lifecycle, status transitions,
    /// scheduling, and member management (roles, removal, ownership transfer).
    /// ProjectMember behaviors are tested through the Project aggregate.
    /// </summary>
    public sealed class ProjectTests
    {
        #region Helper Methods
        private static Project CreateProject(Guid? ownerId = null, DateOnly? startDate = null, DateOnly? dueDate = null)
        {
            var owner = ownerId ?? Guid.NewGuid();
            return new Project(Guid.NewGuid(), Guid.NewGuid(), "Test Project", owner,
                startDate: startDate, dueDate: dueDate);
        }

        private static Project CreateProjectInStatus(ProjectStatus status)
        {
            var project = CreateProject();

            if (status != ProjectStatus.Active)
                project.ChangeStatus(status);

            return project;
        }

        private static void SetStatus(ProjectMember member, ProjectMemberStatus status)
        {
            var property = typeof(ProjectMember).GetProperty(nameof(ProjectMember.Status),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
            property.SetValue(member, status);
        }

        private static void SetId(Entity<long> entity, long id)
        {
            var property = typeof(Entity<long>).GetProperty(nameof(Entity<long>.Id),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;

            property.SetValue(entity, id);
        }
        #endregion

        #region Constructor
        [Fact]
        public void Constructor_WithValidData_CreatesActiveProjectWithOwnerMembership()
        {
            var ownerId = Guid.NewGuid();
            var before = DateTime.UtcNow;

            var project = CreateProject(ownerId);

            Assert.Equal("Test Project", project.Name);
            Assert.Equal(ownerId, project.OwnerUserId);
            Assert.Equal(ownerId, project.CreatedBy);
            Assert.Equal(ProjectStatus.Active, project.Status);
            Assert.InRange(project.CreatedAtUtc, before, DateTime.UtcNow);

            var ownerMembership = Assert.Single(project.Members);

            Assert.Equal(project.Id, ownerMembership.ProjectId);
            Assert.Equal(ownerId, ownerMembership.UserId);
            Assert.Equal(ProjectRole.Owner, ownerMembership.ProjectRole);
            Assert.Equal(ProjectMemberStatus.Active, ownerMembership.Status);
        }

        [Fact]
        public void Constructor_WithValidDates_SetsDates()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = today.AddDays(1);
            var due = today.AddDays(10);

            var project = CreateProject(startDate: start, dueDate: due);

            Assert.Equal(start, project.StartDate);
            Assert.Equal(due, project.DueDate);
        }

        [Fact]
        public void Constructor_WithPastStartDate_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            Assert.Throws<DomainException>(() => CreateProject(startDate: today.AddDays(-1)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankName_Throws(string? name)
        {
            Assert.Throws<DomainException>(() =>
                new Project(Guid.NewGuid(), Guid.NewGuid(), name!, Guid.NewGuid()));
        }
        #endregion

        #region Rename
        [Fact]
        public void Rename_WithValidName_UpdatesNameAndTimestamp()
        {
            var project = CreateProject();
            var before = DateTime.UtcNow;

            project.Rename("Renamed Project");

            Assert.Equal("Renamed Project", project.Name);
            Assert.NotNull(project.UpdatedAtUtc);
            Assert.InRange(project.UpdatedAtUtc!.Value, before, DateTime.UtcNow);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Rename_WithBlankName_Throws(string? name)
        {
            var project = CreateProject();

            Assert.Throws<DomainException>(() => project.Rename(name!));
        }

        [Fact]
        public void Rename_WhenDeleted_Throws()
        {
            var project = CreateProject();
            project.SoftDelete();

            Assert.Throws<DomainException>(() => project.Rename("New Name"));
        }
        #endregion

        #region Update Description
        [Fact]
        public void UpdateDescription_WithValue_UpdatesAndTouches()
        {
            var project = CreateProject();

            project.UpdateDescription("New description");

            Assert.Equal("New description", project.Description);
            Assert.NotNull(project.UpdatedAtUtc);
        }

        [Fact]
        public void UpdateDescription_WithNull_ClearsDescription()
        {
            var project = CreateProject();
            project.UpdateDescription("Initial");

            project.UpdateDescription(null);

            Assert.Null(project.Description);
        }

        [Fact]
        public void UpdateDescription_WhenDeleted_Throws()
        {
            var project = CreateProject();
            project.SoftDelete();

            Assert.Throws<DomainException>(() => project.UpdateDescription("New"));
        }
        #endregion

        #region Schedule
        [Fact]
        public void Schedule_WithValidDates_UpdatesDatesAndTouches()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var project = CreateProject();

            project.Schedule(today.AddDays(1), today.AddDays(10));

            Assert.Equal(today.AddDays(1), project.StartDate);
            Assert.Equal(today.AddDays(10), project.DueDate);
            Assert.NotNull(project.UpdatedAtUtc);
        }

        [Fact]
        public void Schedule_WithOnlyDueDate_KeepsExistingStartDate()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = today.AddDays(1);
            var project = CreateProject(startDate: start);

            project.Schedule(null, today.AddDays(10));

            Assert.Equal(start, project.StartDate);
            Assert.Equal(today.AddDays(10), project.DueDate);
        }

        [Fact]
        public void Schedule_WithDueBeforeStart_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var project = CreateProject();

            Assert.Throws<DomainException>(() => project.Schedule(today.AddDays(10), today.AddDays(1)));
        }

        [Fact]
        public void Schedule_WithPastStartDate_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var project = CreateProject();

            Assert.Throws<DomainException>(() => project.Schedule(today.AddDays(-1), today.AddDays(10)));
        }

        [Fact]
        public void Schedule_WithPastDueDate_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var project = CreateProject();

            Assert.Throws<DomainException>(() => project.Schedule(today.AddDays(1), today.AddDays(-1)));
        }

        [Fact]
        public void Schedule_WhenDeleted_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var project = CreateProject();
            project.SoftDelete();

            Assert.Throws<DomainException>(() => project.Schedule(today.AddDays(1), today.AddDays(10)));
        }
        #endregion

        #region Change Status
        [Theory]
        [InlineData(ProjectStatus.Active, ProjectStatus.OnHold)]
        [InlineData(ProjectStatus.Active, ProjectStatus.Completed)]
        [InlineData(ProjectStatus.Active, ProjectStatus.Archived)]
        [InlineData(ProjectStatus.OnHold, ProjectStatus.Active)]
        [InlineData(ProjectStatus.OnHold, ProjectStatus.Completed)]
        [InlineData(ProjectStatus.OnHold, ProjectStatus.Archived)]
        [InlineData(ProjectStatus.Completed, ProjectStatus.Archived)]
        [InlineData(ProjectStatus.Completed, ProjectStatus.Active)]
        [InlineData(ProjectStatus.Archived, ProjectStatus.Active)]
        public void ChangeStatus_WithValidTransition_ChangesStatusAndTouches(ProjectStatus from, ProjectStatus to)
        {
            var project = CreateProjectInStatus(from);

            project.ChangeStatus(to);

            Assert.Equal(to, project.Status);
            Assert.NotNull(project.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(ProjectStatus.Completed, ProjectStatus.OnHold)]
        [InlineData(ProjectStatus.Archived, ProjectStatus.OnHold)]
        [InlineData(ProjectStatus.Archived, ProjectStatus.Completed)]
        public void ChangeStatus_WithInvalidTransition_ThrowsAndKeepsStatus(ProjectStatus from, ProjectStatus to)
        {
            var project = CreateProjectInStatus(from);

            Assert.Throws<DomainException>(() => project.ChangeStatus(to));
            Assert.Equal(from, project.Status);
        }

        [Fact]
        public void ChangeStatus_WithSameStatus_Throws()
        {
            var project = CreateProject();

            Assert.Throws<DomainException>(() => project.ChangeStatus(ProjectStatus.Active));
        }

        [Fact]
        public void ChangeStatus_WhenDeleted_Throws()
        {
            var project = CreateProject();
            project.SoftDelete();

            Assert.Throws<DomainException>(() => project.ChangeStatus(ProjectStatus.OnHold));
        }
        #endregion

        #region Soft Delete
        [Fact]
        public void SoftDelete_WhenNotDeleted_SetsDeleted()
        {
            var project = CreateProject();
            var before = DateTime.UtcNow;

            project.SoftDelete();

            Assert.NotNull(project.DeletedAtUtc);
            Assert.InRange(project.DeletedAtUtc!.Value, before, DateTime.UtcNow);
        }

        [Fact]
        public void SoftDelete_WhenAlreadyDeleted_Throws()
        {
            var project = CreateProject();
            project.SoftDelete();

            Assert.Throws<DomainException>(() => project.SoftDelete());
        }
        #endregion

        #region Add Member
        [Fact]
        public void AddMember_WithValidData_CreatesMembership()
        {
            var project = CreateProject();
            var userId = Guid.NewGuid();
            var before = DateTime.UtcNow;

            var member = project.AddMember(userId, ProjectRole.Member);

            Assert.Equal(project.Id, member.ProjectId);
            Assert.Equal(userId, member.UserId);
            Assert.Equal(ProjectRole.Member, member.ProjectRole);
            Assert.Equal(ProjectMemberStatus.Active, member.Status);
            Assert.Null(member.AddedBy);
            Assert.InRange(member.AddedAtUtc, before, DateTime.UtcNow);
        }

        [Fact]
        public void AddMember_WithAddedBy_SetsAddedBy()
        {
            var project = CreateProject();
            var addedBy = Guid.NewGuid();

            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Admin, addedBy);

            Assert.Equal(addedBy, member.AddedBy);
        }

        [Fact]
        public void AddMember_WhenUserHasActiveMembership_Throws()
        {
            var project = CreateProject();
            var userId = Guid.NewGuid();
            project.AddMember(userId, ProjectRole.Member);

            Assert.Throws<DomainException>(() => project.AddMember(userId, ProjectRole.Viewer));
        }

        [Fact]
        public void AddMember_WhenUserHasSuspendedMembership_Throws()
        {
            var project = CreateProject();
            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);
            SetStatus(member, ProjectMemberStatus.Suspended);

            Assert.Throws<DomainException>(() => project.AddMember(member.UserId, ProjectRole.Viewer));
        }

        [Fact]
        public void AddMember_WhenUserHasRemovedMembership_CreatesNewMembership()
        {
            var project = CreateProject();
            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);
            SetStatus(member, ProjectMemberStatus.Removed);

            var newMember = project.AddMember(member.UserId, ProjectRole.Viewer);

            Assert.Equal(ProjectMemberStatus.Active, newMember.Status);
            Assert.Equal(ProjectRole.Viewer, newMember.ProjectRole);
            Assert.Equal(2, project.Members.Count(m => m.UserId == member.UserId));
        }

        [Fact]
        public void AddMember_WithOwnerRole_Throws()
        {
            var project = CreateProject();

            Assert.Throws<DomainException>(() => project.AddMember(Guid.NewGuid(), ProjectRole.Owner));
        }

        [Fact]
        public void AddMember_WhenDeleted_Throws()
        {
            var project = CreateProject();
            project.SoftDelete();

            Assert.Throws<DomainException>(() => project.AddMember(Guid.NewGuid(), ProjectRole.Member));
        }
        #endregion

        #region Remove Member
        [Fact]
        public void RemoveMember_WhenMemberNotFound_Throws()
        {
            var project = CreateProject();

            Assert.Throws<DomainException>(() => project.RemoveMember(999999, Guid.NewGuid()));
        }

        [Fact]
        public void RemoveMember_WhenOwner_Throws()
        {
            var project = CreateProject();

            var owner = Assert.Single(project.Members);

            Assert.Throws<DomainException>(() => project.RemoveMember(owner.Id, Guid.NewGuid()));
        }

        [Fact]
        public void RemoveMember_WhenActive_SucceedsAndRaisesEvent()
        {
            var project = CreateProject();
            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);
            SetId(member, 1);
            var removedBy = Guid.NewGuid();
            var before = DateTime.UtcNow;

            project.RemoveMember(member.Id, removedBy);

            Assert.Equal(ProjectMemberStatus.Removed, member.Status);
            Assert.Equal(removedBy, member.RemovedBy);
            Assert.NotNull(member.RemovedAtUtc);
            Assert.InRange(member.RemovedAtUtc!.Value, before, DateTime.UtcNow);
            Assert.Contains(project.DomainEvents, e => e is ProjectMemberRemovedDomainEvent
            {
                RemovedUserId: var removedUser, ActorUserId: var actor
            } && removedUser == member.UserId && actor == removedBy);
        }

        [Fact]
        public void RemoveMember_WhenSuspended_Succeeds()
        {
            var project = CreateProject();
            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);
            SetId(member, 1);
            SetStatus(member, ProjectMemberStatus.Suspended);

            var removedBy = Guid.NewGuid();

            project.RemoveMember(member.Id, removedBy);

            Assert.Equal(ProjectMemberStatus.Removed, member.Status);
            Assert.Equal(removedBy, member.RemovedBy);
        }

        [Fact]
        public void RemoveMember_WhenAlreadyRemoved_Throws()
        {
            var project = CreateProject();
            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);
            SetId(member, 1);
            project.RemoveMember(member.Id, Guid.NewGuid());

            Assert.Throws<DomainException>(() => project.RemoveMember(member.Id, Guid.NewGuid()));
        }
        #endregion

        #region Change Member Role
        [Fact]
        public void ChangeMemberRole_WhenMemberNotFound_Throws()
        {
            var project = CreateProject();

            Assert.Throws<DomainException>(() => project.ChangeMemberRole(999999, ProjectRole.Admin, Guid.NewGuid()));
        }

        [Fact]
        public void ChangeMemberRole_WhenOwner_Throws()
        {
            var project = CreateProject();

            var owner = Assert.Single(project.Members);

            Assert.Throws<DomainException>(() => project.ChangeMemberRole(owner.Id, ProjectRole.Admin, project.OwnerUserId));
        }

        [Fact]
        public void ChangeMemberRole_ToOwnerRole_Throws()
        {
            var project = CreateProject();
            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);
            SetId(member, 1);

            Assert.Throws<DomainException>(() => project.ChangeMemberRole(member.Id, ProjectRole.Owner, project.OwnerUserId));
            Assert.Equal(ProjectRole.Member, member.ProjectRole);
        }

        [Fact]
        public void ChangeMemberRole_WithSameRole_Throws()
        {
            var project = CreateProject();
            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);
            SetId(member, 1);

            Assert.Throws<DomainException>(() => project.ChangeMemberRole(member.Id, ProjectRole.Member, project.OwnerUserId));
        }

        [Fact]
        public void ChangeMemberRole_WhenValid_ChangesRoleAndRaisesEvent()
        {
            var project = CreateProject();
            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);
            SetId(member, 1);
            var changedBy = project.OwnerUserId;

            project.ChangeMemberRole(member.Id, ProjectRole.Admin, changedBy);

            Assert.Equal(ProjectRole.Admin, member.ProjectRole);
            Assert.Contains(project.DomainEvents, e => e is ProjectMemberRoleChangedDomainEvent
            {
                MemberUserId: var userId, ActorUserId: var actor,
                OldRole: ProjectRole.Member, NewRole: ProjectRole.Admin
            } && userId == member.UserId && actor == changedBy);
        }

        [Fact]
        public void ChangeMemberRole_WhenSuspendedMember_ChangesRole()
        {
            var project = CreateProject();
            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);
            SetId(member, 1);
            SetStatus(member, ProjectMemberStatus.Suspended);

            project.ChangeMemberRole(member.Id, ProjectRole.Admin, project.OwnerUserId);

            Assert.Equal(ProjectRole.Admin, member.ProjectRole);
            Assert.Equal(ProjectMemberStatus.Suspended, member.Status);
        }

        [Fact]
        public void ChangeMemberRole_WhenRemovedMember_Throws()
        {
            var project = CreateProject();
            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);
            SetId(member, 1);
            SetStatus(member, ProjectMemberStatus.Removed);

            Assert.Throws<DomainException>(() => project.ChangeMemberRole(member.Id, ProjectRole.Admin, project.OwnerUserId));
        }

        [Fact]
        public void ChangeMemberRole_WhenDeleted_Throws()
        {
            var project = CreateProject();
            var member = project.AddMember(Guid.NewGuid(), ProjectRole.Member);
            SetId(member, 1);
            project.SoftDelete();

            Assert.Throws<DomainException>(() => project.ChangeMemberRole(member.Id, ProjectRole.Admin, project.OwnerUserId));
        }
        #endregion

        #region Transfer Ownership
        [Fact]
        public void TransferOwnership_ByNonOwner_Throws()
        {
            var ownerId = Guid.NewGuid();
            var newOwnerId = Guid.NewGuid();
            var project = CreateProject(ownerId);
            project.AddMember(newOwnerId, ProjectRole.Member);

            Assert.Throws<DomainException>(() => project.TransferOwnership(newOwnerId, Guid.NewGuid()));
            Assert.Equal(ownerId, project.OwnerUserId);
        }

        [Theory]
        [InlineData(ProjectMemberStatus.Suspended)]
        [InlineData(ProjectMemberStatus.Removed)]
        public void TransferOwnership_WhenCurrentOwnerHasNoActiveMembership_Throws(ProjectMemberStatus status)
        {
            var ownerId = Guid.NewGuid();
            var newOwnerId = Guid.NewGuid();
            var project = CreateProject(ownerId);
            project.AddMember(newOwnerId, ProjectRole.Member);

            var currentOwner = Assert.Single(project.Members, m => m.UserId == ownerId);
            SetStatus(currentOwner, status);

            Assert.Throws<DomainException>(() => project.TransferOwnership(newOwnerId, ownerId));
            Assert.Equal(ownerId, project.OwnerUserId);
        }

        [Theory]
        [InlineData(ProjectMemberStatus.Suspended)]
        [InlineData(ProjectMemberStatus.Removed)]
        public void TransferOwnership_WhenNewOwnerIsNotActiveMember_Throws(ProjectMemberStatus status)
        {
            var ownerId = Guid.NewGuid();
            var newOwnerId = Guid.NewGuid();
            var project = CreateProject(ownerId);
            var newOwner = project.AddMember(newOwnerId, ProjectRole.Member);
            SetStatus(newOwner, status);

            Assert.Throws<DomainException>(() => project.TransferOwnership(newOwnerId, ownerId));
            Assert.Equal(ownerId, project.OwnerUserId);
        }

        [Fact]
        public void TransferOwnership_WhenSuccessful_TransfersOwnershipAndRaisesEvent()
        {
            var ownerId = Guid.NewGuid();
            var newOwnerId = Guid.NewGuid();
            var project = CreateProject(ownerId);
            var newOwner = project.AddMember(newOwnerId, ProjectRole.Member);

            var currentOwner = Assert.Single(project.Members, m => m.UserId == ownerId);
            var before = DateTime.UtcNow;

            project.TransferOwnership(newOwnerId, ownerId);

            Assert.Equal(newOwnerId, project.OwnerUserId);
            Assert.Equal(ProjectRole.Admin, currentOwner.ProjectRole);
            Assert.Equal(ProjectRole.Owner, newOwner.ProjectRole);
            Assert.InRange(project.UpdatedAtUtc!.Value, before, DateTime.UtcNow);
            Assert.Contains(project.DomainEvents, e => e is ProjectOwnershipTransferredDomainEvent
            {
                PreviousOwnerUserId: var previous, NewOwnerUserId: var current
            } && previous == ownerId && current == newOwnerId);
        }

        [Fact]
        public void TransferOwnership_WhenDeleted_Throws()
        {
            var ownerId = Guid.NewGuid();
            var newOwnerId = Guid.NewGuid();
            var project = CreateProject(ownerId);
            project.AddMember(newOwnerId, ProjectRole.Member);
            project.SoftDelete();

            Assert.Throws<DomainException>(() => project.TransferOwnership(newOwnerId, ownerId));
        }
        #endregion

        #region Domain Events
        [Fact]
        public void Constructor_RaisesProjectCreatedDomainEvent()
        {
            var project = CreateProject();

            Assert.Contains(project.DomainEvents, e => e is ProjectCreatedDomainEvent { ActorUserId: var actor } && actor == project.CreatedBy);
        }
        #endregion
    }
}
