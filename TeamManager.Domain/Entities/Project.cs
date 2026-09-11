using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities
{

    public class Project : Entity<Guid>
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

            if (startDate.HasValue && dueDate.HasValue && dueDate.Value < startDate.Value)
                throw new DomainException("A project's due date cannot be before its start date.");

            if (startDate.HasValue && startDate.Value < today)
                throw new DomainException("Project start date cannot be in the past.");

            if (dueDate.HasValue && dueDate.Value < today)
                throw new DomainException("Project due date cannot be in the past.");

            StartDate = startDate ?? StartDate;
            DueDate = dueDate ?? DueDate;
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

        public void SoftDelete()
        {
            EnsureNotDeleted("Project is already deleted");
            DeletedAtUtc = DateTime.UtcNow;
        }

        public ProjectMember AddMember(Guid userId, ProjectRole role, Guid? addedBy = null)
        {
            EnsureNotDeleted("cannot add member to deleted project");

            if (_members.Any(m => m.UserId == userId && m.Status == ProjectMemberStatus.Active))
                throw new DomainException("This user already has an active membership in the project.");

            if (role == ProjectRole.Owner)
                throw new DomainException("Ownership cannot be assigned");

            var member = new ProjectMember(Id, userId, role, addedBy);
            _members.Add(member);
            return member;
        }

        public void RemoveMember(Guid userId)
        {
            var member = _members.FirstOrDefault(m => m.UserId == userId && m.Status == ProjectMemberStatus.Active);

            if (member is null)
                throw new DomainException("This user does not have an active membership in the project.");
            if (member.ProjectRole == ProjectRole.Owner)
                throw new DomainException("cannot remove project owner from project");

            member.Remove();
        }

        private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

        private void EnsureNotDeleted(string Message)
        {
            if (DeletedAtUtc.HasValue)
                throw new DomainException(Message);
        }
    }
}