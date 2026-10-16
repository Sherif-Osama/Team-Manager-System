using TeamManager.Application.Abstractions.AuditLog;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Infrastructure.Services.AuditLog
{
    public sealed class AuditLogWriter(IAuditLogRepository auditLogRepository, ICurrentUser currentUser) : IAuditLogWriter
    {
        public async Task WriteAsync(string action, string entityType, string entityId, string? details, CancellationToken cancellationToken)
        {
            var auditLog = new Domain.Entities.AuditLog(action, entityType, entityId, currentUser.UserId, details,
                currentUser.IpAddress);

            await auditLogRepository.AddAsync(auditLog, cancellationToken);
        }
    }
}