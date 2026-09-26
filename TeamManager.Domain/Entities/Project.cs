using TeamManager.Domain.Common;
using TeamManager.Domain.Common.Events;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities
{

    public class Project : AggregateRoot<Guid>
    {
        private readonly List<ProjectMember> _members = new();
        private readonly List<TaskItem> _tasks = new();

        public Guid TeamId { get; private set; }
        public Team Team { get; private set; } = null!;
        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public ProjectStatus Status { get; private set; }
        public DateOnly? StartDate { get; private set; }
        public DateOnly? DueDate { get; private set; }
        public Guid OwnerUserId { get; private set; }
        public User Owner { get; private set; } = null!;
        public DateTime CreatedAtUtc { get; private set; }
        public Guid CreatedBy { get; private set; }
        public User Creator { get; private set; } = null!;
        public DateTime? UpdatedAtUtc { get; private set; }
        public DateTime? DeletedAtUtc { get; private set; }

        public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();
        public IReadOnlyCollection<TaskItem> Tasks => _tasks.AsReadOnly();

        private Project() { }

        public Project(Guid id, Guid teamId, string name, Guid ownerUserId, string? description = null,
            DateOnly? startDate = null, DateOnly? dueDate = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("A project must have a name.");

            Id = id;
            TeamId = teamId;
            Name = name;
            Description = description;
            OwnerUserId = ownerUserId;
            CreatedBy = ownerUserId;
            Status = ProjectStatus.Active;
            CreatedAtUtc = DateTime.UtcNow;

            if (startDate.HasValue || dueDate.HasValue)
                Schedule(startDate, dueDate);

            var ownership = new ProjectMember(id, ownerUserId, ProjectRole.Owner);
            _members.Add(ownership);
        }

        public void Rename(string name)
        {
            EnsureNotDeleted("Cannot modify a deleted project.");

            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("A project must have a name.");

            Name = name;
            Touch();
        }

        public void UpdateDescription(string? description)
        {
            EnsureNotDeleted("Cannot modify a deleted project.");
            Description = description;
            Touch();
        }

        public void Schedule(DateOnly? startDate, DateOnly? dueDate)
        {
            EnsureNotDeleted("Cannot modify a deleted project.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var effectiveStart = startDate ?? StartDate;
            var effectiveDue = dueDate ?? DueDate;

            if (effectiveStart.HasValue && effectiveDue.HasValue && effectiveDue.Value < effectiveStart.Value)
                throw new DomainException("A project's due date cannot be before its start date.");

            if (startDate.HasValue && startDate.Value < today)
                throw new DomainException("Project start date cannot be in the past.");

            if (dueDate.HasValue && dueDate.Value < today)
                throw new DomainException("Project due date cannot be in the past.");

            StartDate = effectiveStart;
            DueDate = effectiveDue;
            Touch();
        }

        public void ChangeStatus(ProjectStatus status)
        {
            EnsureNotDeleted("Cannot modify a deleted project.");

            if (Status == status)
                throw new DomainException($"project already has status {status}");

            if (!CanTransitionTo(status))
                throw new DomainException($"Project cannot transition from {Status} to {status}.");

            Status = status;
            Touch();
        }

        private bool CanTransitionTo(ProjectStatus newStatus)
        {
            return Status switch
            {
                ProjectStatus.Active => newStatus is ProjectStatus.OnHold or ProjectStatus.Completed or ProjectStatus.Archived,

                ProjectStatus.OnHold => newStatus is ProjectStatus.Active or ProjectStatus.Completed or ProjectStatus.Archived,

                ProjectStatus.Completed => newStatus is ProjectStatus.Archived or ProjectStatus.Active,

                ProjectStatus.Archived => newStatus is ProjectStatus.Active,

                _ => false
            };
        }

        public void ChangeMemberRole(long memberId, ProjectRole role)
        {
            EnsureNotDeleted("Cannot modify a deleted project.");

            if (role == ProjectRole.Owner)
                throw new DomainException("Ownership must be transferred through the ownership transfer process.");

            var member = _members.FirstOrDefault(m => m.Id == memberId && ProjectMemberStatuses.Occupied.Contains(m.Status));

            if (member is null)
                throw new DomainException("This user does not have an active membership in the project.");

            if (member.ProjectRole == ProjectRole.Owner)
                throw new DomainException("Ownership cannot be changed");

            if (member.ProjectRole == role)
                throw new DomainException("The member already has this role.");


            member.ChangeRole(role);
        }

        public void TransferOwnership(Guid newOwnerUserId)
        {
            EnsureNotDeleted("Cannot modify a deleted project.");

            var currentOwner = _members.FirstOrDefault(m => m.UserId == OwnerUserId && m.Status == ProjectMemberStatus.Active);

            if (currentOwner is null)
                throw new DomainException("The current project owner must have an active membership.");

            var newOwner = _members.FirstOrDefault(m => m.UserId == newOwnerUserId && m.Status == ProjectMemberStatus.Active);

            if (newOwner is null)
                throw new DomainException("The new owner must already be an active project member.");

            currentOwner.ChangeRole(ProjectRole.Admin);

            newOwner.PromoteToOwner();

            OwnerUserId = newOwnerUserId;

            Touch();
        }

        public void SoftDelete()
        {
            EnsureNotDeleted("Project is already deleted");
            DeletedAtUtc = DateTime.UtcNow;
        }

        public ProjectMember AddMember(Guid userId, ProjectRole role, Guid? addedBy = null)
        {
            EnsureNotDeleted("cannot add member to deleted project");

            if (_members.Any(m => m.UserId == userId && ProjectMemberStatuses.Occupied.Contains(m.Status)))
                throw new DomainException("This user already has an active membership in the project.");

            if (role == ProjectRole.Owner)
                throw new DomainException("Ownership cannot be assigned");

            var member = new ProjectMember(Id, userId, role, addedBy);

            _members.Add(member);
            return member;
        }

        public void RemoveMember(long memberId, Guid removedBy)
        {
            var member = _members.FirstOrDefault(m => m.Id == memberId && ProjectMemberStatuses.Occupied.Contains(m.Status));

            if (member is null)
                throw new DomainException("This user does not have an active membership in the project.");

            if (member.ProjectRole == ProjectRole.Owner)
                throw new DomainException("cannot remove project owner from project");

            member.Remove(removedBy);
            AddDomainEvent(new ProjectMemberRemovedDomainEvent(Id, member.UserId));
        }

        private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

        private void EnsureNotDeleted(string message)
        {
            if (DeletedAtUtc.HasValue)
                throw new DomainException(message);
        }
    }
}