using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Entities;
namespace TeamManager.Infrastructure.Persistence.Repositories
{
    public sealed class AuditLogRepository(TeamManagerDbContext context) : IAuditLogRepository
    {
        public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken)
        {
            await context.AuditLogs.AddAsync(auditLog, cancellationToken);
        }
    }
}