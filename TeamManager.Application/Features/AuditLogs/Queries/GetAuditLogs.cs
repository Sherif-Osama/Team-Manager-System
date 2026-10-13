using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.AuditLogs.Queries
{
    public sealed record GetAuditLogsQuery(string? Action = null, string? EntityType = null, string? EntityId = null,
        Guid? ActorUserId = null, DateTime? FromUtc = null, DateTime? ToUtc = null, int Page = 1, int PageSize = 20) : IRequest<GetAuditLogsResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ViewAuditLog;
    }
}
