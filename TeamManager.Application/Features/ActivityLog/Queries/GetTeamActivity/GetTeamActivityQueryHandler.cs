using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.ActivityLog.Queries.GetTeamActivity
{
    public sealed class GetTeamActivityQueryHandler(IApplicationDbContext context, ITeamRepository teamRepository, ICurrentUser currentUser) : IRequestHandler<GetTeamActivityQuery, GetTeamActivityResponse>
    {
        public async Task<GetTeamActivityResponse> Handle(GetTeamActivityQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || !currentUser.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var query = context.ActivityLogs.AsNoTracking().Where(x => x.TeamId == request.TeamId &&
                    x.Team.DeletedAtUtc == null && (x.ProjectId == null || x.Project!.DeletedAtUtc == null));

            var isTeamOwner = await teamRepository.HasActiveRoleAsync(request.TeamId, userId, [TeamRole.Owner], cancellationToken);

            if (!isTeamOwner)
            {
                query = query.Where(x => x.ProjectId == null || x.Project!.Members.Any(pm =>
                pm.UserId == userId && pm.Status == ProjectMemberStatus.Active && pm.User.IsActive));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(x => new GetTeamActivityItem(x.Id, x.ActivityType, x.ActorUserId, x.Actor.DisplayName,
                x.ProjectId, x.Project != null ? x.Project.Name : null, x.EntityType, x.EntityId, x.Metadata, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetTeamActivityResponse(items, totalCount, request.Page, request.PageSize);
        }
    }
}