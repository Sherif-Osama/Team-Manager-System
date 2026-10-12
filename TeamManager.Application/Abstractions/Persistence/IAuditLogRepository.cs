namespace TeamManager.Application.Abstractions.Persistence
{
    public interface IAuditLogRepository
    {
        Task AddAsync(Domain.Entities.AuditLog auditLog, CancellationToken cancellationToken);
    }
}