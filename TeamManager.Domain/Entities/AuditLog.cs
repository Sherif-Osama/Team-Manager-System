using TeamManager.Domain.Common;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class AuditLog : Entity<long>
{
    public Guid? ActorUserId { get; private set; }
    public User? Actor { get; private set; }
    public string Action { get; private set; } = null!;
    public string EntityType { get; private set; } = null!;
    public string EntityId { get; private set; } = null!;
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AuditLog()
    {
    }

    public AuditLog(string action, string entityType, string entityId, Guid? actorUserId = null,
        string? details = null, string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new DomainException("An audit log entry must have an action.");
        if (string.IsNullOrWhiteSpace(entityType))
            throw new DomainException("An audit log entry must have an entity type.");
        if (string.IsNullOrWhiteSpace(entityId))
            throw new DomainException("An audit log entry must have an entity id.");

        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        ActorUserId = actorUserId;
        Details = details;
        IpAddress = ipAddress;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
