using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.Project.Queries.GetProjects
{
    public sealed class GetProjectsQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetProjectsQuery, GetProjectsResponse>
    {
        public async Task<GetProjectsResponse> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
        {
            var query = context.Projects.AsNoTracking().Where(x => x.DeletedAtUtc == null);

            if (request.Status.HasValue)
                query = query.Where(x => x.Status == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x => x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var projects = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(x => new ProjectListItem(x.Id, x.TeamId, x.Name, x.Team.Name, x.Description,
                x.Status, x.StartDate, x.DueDate, x.OwnerUserId, x.Owner.DisplayName, x.CreatedBy, x.Creator.DisplayName,
                x.Members.Count(m => m.Status == ProjectMemberStatus.Active),
                x.Tasks.Count(t => t.DeletedAtUtc == null),
                x.CreatedAtUtc, x.UpdatedAtUtc)).ToListAsync(cancellationToken);

            return new GetProjectsResponse(projects, totalCount, request.Page, request.PageSize);
        }
    }
}