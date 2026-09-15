using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Queries.GetProjectMembers
{
    public sealed class GetProjectMembersQueryHandler(IApplicationDbContext context) : IRequestHandler<GetProjectMembersQuery, GetProjectMembersResponse>
    {
        public async Task<GetProjectMembersResponse> Handle(GetProjectMembersQuery request, CancellationToken cancellationToken)
        {
            var query = context.ProjectMembers.AsNoTracking().Where(pm => pm.ProjectId == request.ProjectId && pm.Project.DeletedAtUtc == null);

            if (request.Role.HasValue)
                query = query.Where(pm => pm.ProjectRole == request.Role.Value);

            if (request.Status.HasValue)
                query = query.Where(pm => pm.Status == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(pm => pm.User.DisplayName.Contains(request.Search.Trim()));

            var totalCount = await query.CountAsync(cancellationToken);

            var members = await query.OrderByDescending(pm => pm.AddedAtUtc).
                Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(pm =>
                new ProjectMemberListItem(pm.UserId, pm.User.DisplayName, pm.ProjectRole, pm.Status, pm.AddedAtUtc, pm.AddedBy,
                pm.AddedByUser != null ? pm.AddedByUser.DisplayName : null, pm.RemovedAtUtc, pm.RemovedBy,
                pm.RemovedByUser != null ? pm.RemovedByUser.DisplayName : null)).ToListAsync(cancellationToken);

            return new GetProjectMembersResponse(members, totalCount, request.Page, request.PageSize);
        }
    }
}