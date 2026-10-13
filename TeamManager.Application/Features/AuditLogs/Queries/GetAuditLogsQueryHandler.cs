using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.AuditLogs.Queries
{
    public sealed class GetAuditLogsQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetAuditLogsQuery, GetAuditLogsResponse>
    {
        public async Task<GetAuditLogsResponse> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var query = context.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Action))
                query = query.Where(x => x.Action == request.Action.Trim());

            if (!string.IsNullOrWhiteSpace(request.EntityType))
                query = query.Where(x => x.EntityType == request.EntityType.Trim());

            if (!string.IsNullOrWhiteSpace(request.EntityId))
                query = query.Where(x => x.EntityId == request.EntityId.Trim());


            if (request.ActorUserId.HasValue)
                query = query.Where(x => x.ActorUserId == request.ActorUserId.Value);

            if (request.FromUtc.HasValue)
                query = query.Where(x => x.CreatedAtUtc >= request.FromUtc.Value);

            if (request.ToUtc.HasValue)
                query = query.Where(x => x.CreatedAtUtc <= request.ToUtc.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(x => new AuditLogItem(x.Id, x.Action, x.EntityType, x.EntityId, x.ActorUserId,
                x.Actor != null ? x.Actor.DisplayName : null, x.Details, x.IpAddress, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetAuditLogsResponse(items, request.Page, request.PageSize, totalCount);
        }
    }
}