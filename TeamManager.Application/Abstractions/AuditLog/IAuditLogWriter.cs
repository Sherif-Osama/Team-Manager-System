namespace TeamManager.Application.Abstractions.AuditLog
{
    public interface IAuditLogWriter
    {
        Task WriteAsync(string action, string entityType, string entityId, string? details, CancellationToken cancellationToken);
    }
}