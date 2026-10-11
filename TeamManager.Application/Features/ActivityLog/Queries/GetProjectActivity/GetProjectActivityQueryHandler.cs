using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.ActivityLog.Queries.GetProjectActivity
{
    public sealed class GetProjectActivityQueryHandler(IApplicationDbContext context) : IRequestHandler<GetProjectActivityQuery, GetProjectActivityResponse>
    {
        public async Task<GetProjectActivityResponse> Handle(GetProjectActivityQuery request, CancellationToken cancellationToken)
        {
            var query = context.ActivityLogs.AsNoTracking().Where(x => x.ProjectId == request.ProjectId && x.Project != null &&
                    x.Project.DeletedAtUtc == null);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(x => new GetProjectActivityItem(x.Id, x.ActivityType, x.ActorUserId,
                x.Actor.DisplayName, x.EntityType, x.EntityId, x.Metadata, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetProjectActivityResponse(items, totalCount, request.Page, request.PageSize);
        }
    }
}