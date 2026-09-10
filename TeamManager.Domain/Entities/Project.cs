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

        public Project(Guid id, Guid teamId, string name, Guid ownerUserId, string? description = null)
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

            var ownership = new ProjectMember(id, ownerUserId, TeamRole.Owner);

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

        public void Schedule(DateOnly? startDate, DateOnly? dueDate)
        {
            EnsureNotDeleted("Cannot modify a deleted project.");

            if (startDate.HasValue && dueDate.HasValue && dueDate.Value < startDate.Value)
                throw new DomainException("A project's due date cannot be before its start date.");

            StartDate = startDate;
            DueDate = dueDate;
            Touch();
        }

        public void ChangeStatus(ProjectStatus status)
        {
            EnsureNotDeleted("Cannot modify a deleted project.");

            if (Status == status)
                throw new DomainException($"project already has status {status}");

            Status = status;
            Touch();
        }

        public void SoftDelete()
        {
            EnsureNotDeleted("Project is already deleted");
            DeletedAtUtc = DateTime.UtcNow;
        }

        public ProjectMember AddMember(Guid userId, TeamRole role, Guid? addedBy = null)
        {
            EnsureNotDeleted("cannot add member to deleted project");

            if (_members.Any(m => m.UserId == userId && m.Status == ProjectMemberStatus.Active))
                throw new DomainException("This user already has an active membership in the project.");

            if (role == TeamRole.Owner)
                throw new DomainException("Ownership cannot be assigned via AddMember. Use TransferOwnership instead.");

            var member = new ProjectMember(Id, userId, role, addedBy);
            _members.Add(member);
            return member;
        }

        public void RemoveMember(Guid userId)
        {
            var member = _members.FirstOrDefault(m => m.UserId == userId && m.Status == ProjectMemberStatus.Active);

            if (member is null)
                throw new DomainException("This user does not have an active membership in the project.");
            if (member.TeamRole == TeamRole.Owner)
                throw new DomainException("cannot remove project owner from project");

            member.Remove();
        }

        public void TransferOwnership(Guid newOwnerUserId)
        {
            EnsureNotDeleted("Cannot modify a deleted project.");

            if (OwnerUserId == newOwnerUserId)
                throw new DomainException("The specified user is already the project owner.");

            var currentOwner = _members.FirstOrDefault(m => m.UserId == OwnerUserId && m.Status == ProjectMemberStatus.Active);

            if (currentOwner is null)
                throw new DomainException("The current project owner must have an active membership.");

            var newOwner = _members.FirstOrDefault(m => m.UserId == newOwnerUserId && m.Status == ProjectMemberStatus.Active);

            if (newOwner is null)
                throw new DomainException("The new owner must be an active project member.");

            currentOwner.ChangeRole(TeamRole.Admin);
            newOwner.PromoteToOwner();
            OwnerUserId = newOwnerUserId;
            Touch();
        }

        private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

        private void EnsureNotDeleted(string Message)
        {
            if (DeletedAtUtc.HasValue)
                throw new DomainException(Message);
        }
    }
}