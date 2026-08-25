using TeamManager.Domain.Common;
using TeamManager.Domain.Enums;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class Notification : Entity<long>
{
    public Guid RecipientUserId { get; private set; }
    public User Recipient { get; private set; } = null!;
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Body { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public string? RelatedEntityId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Notification() { }

    public Notification(Guid recipientUserId, NotificationType type, string title, string? body = null,
        string? relatedEntityType = null, string? relatedEntityId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("A notification must have a title.");

        RecipientUserId = recipientUserId;
        Type = type;
        Title = title;
        Body = body;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsRead()
    {
        if (IsRead) return;

        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
    }
}
