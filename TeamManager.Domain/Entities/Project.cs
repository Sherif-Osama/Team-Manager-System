using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

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

    public Project(Guid id, Guid teamId, string name, Guid ownerUserId, Guid createdBy,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A project must have a name.");

        Id = id;
        TeamId = teamId;
        Name = name;
        Description = description;
        OwnerUserId = ownerUserId;
        CreatedBy = createdBy;
        Status = ProjectStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A project must have a name.");

        Name = name;
        Touch();
    }

    public void Schedule(DateOnly? startDate, DateOnly? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && dueDate.Value < startDate.Value)
            throw new DomainException("A project's due date cannot be before its start date.");

        StartDate = startDate;
        DueDate = dueDate;
        Touch();
    }

    public void ChangeStatus(ProjectStatus status)
    {
        Status = status;
        Touch();
    }

    public void Archive()
    {
        Status = ProjectStatus.Archived;
        Touch();
    }

    public void SoftDelete()
    {
        DeletedAtUtc = DateTime.UtcNow;
    }

    public ProjectMember AddMember(Guid userId, TeamRole role, Guid? addedBy = null)
    {
        if (_members.Any(m => m.UserId == userId && m.Status == ProjectMemberStatus.Active))
            throw new DomainException("This user already has an active membership in the project.");

        var member = new ProjectMember(Id, userId, role, addedBy);
        _members.Add(member);
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId && m.Status == ProjectMemberStatus.Active);
        if (member is null)
            throw new DomainException("This user does not have an active membership in the project.");

        member.Remove();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
