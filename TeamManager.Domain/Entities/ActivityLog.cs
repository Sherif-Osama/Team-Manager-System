using TeamManager.Domain.Common;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class ActivityLog : Entity<long>
{
    public Guid TeamId { get; private set; }
    public Team Team { get; private set; } = null!;
    public Guid? ProjectId { get; private set; }
    public Project? Project { get; private set; }
    public Guid ActorUserId { get; private set; }
    public User Actor { get; private set; } = null!;
    public string ActivityType { get; private set; } = null!;
    public string EntityType { get; private set; } = null!;
    public string EntityId { get; private set; } = null!;
    public string? Metadata { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ActivityLog()
    {
    }

    public ActivityLog(Guid teamId, Guid actorUserId, string activityType, string entityType, string entityId,
        Guid? projectId = null, string? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(activityType))
            throw new DomainException("An activity log entry must have an activity type.");
        if (string.IsNullOrWhiteSpace(entityType))
            throw new DomainException("An activity log entry must have an entity type.");
        if (string.IsNullOrWhiteSpace(entityId))
            throw new DomainException("An activity log entry must have an entity id.");

        TeamId = teamId;
        ProjectId = projectId;
        ActorUserId = actorUserId;
        ActivityType = activityType;
        EntityType = entityType;
        EntityId = entityId;
        Metadata = metadata;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
