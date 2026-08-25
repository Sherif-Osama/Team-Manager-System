using TeamManager.Domain.Common;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class TaskAttachment : Entity<long>
{
    public long TaskId { get; private set; }
    public TaskItem Task { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public string StorageKey { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public string? FileHash { get; private set; }
    public Guid UploadedBy { get; private set; }
    public User UploadedByUser { get; private set; } = null!;
    public DateTime UploadedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private TaskAttachment()
    {
    }

    internal TaskAttachment(long taskId, string originalFileName, string storageKey, string contentType,
        long sizeBytes, Guid uploadedBy, string? fileHash = null)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new DomainException("An attachment must have an original file name.");
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new DomainException("An attachment must have a storage key.");
        if (string.IsNullOrWhiteSpace(contentType))
            throw new DomainException("An attachment must have a content type.");
        if (sizeBytes < 0)
            throw new DomainException("An attachment's size cannot be negative.");

        TaskId = taskId;
        OriginalFileName = originalFileName;
        StorageKey = storageKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedBy = uploadedBy;
        FileHash = fileHash;
        UploadedAtUtc = DateTime.UtcNow;
    }

    public void SoftDelete() => DeletedAtUtc = DateTime.UtcNow;
}
