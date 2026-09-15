using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Projects.ProjectMembers.Queries.GetMyProjects
{
    public sealed class GetMyProjectsQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
        : IRequestHandler<GetMyProjectsQuery, GetMyProjectsResponse>
    {
        public async Task<GetMyProjectsResponse> Handle(GetMyProjectsQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var query = context.ProjectMembers.AsNoTracking().Where(pm => pm.UserId == userId && ProjectMemberStatuses.Occupied.Contains(pm.Status)
            && pm.Project.DeletedAtUtc == null);

            if (request.Role.HasValue)
                query = query.Where(pm => pm.ProjectRole == request.Role.Value);


            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(pm => pm.Project.Name.Contains(search) ||
                (pm.Project.Description != null && pm.Project.Description.Contains(search)));
            }

            var stopwatch = Stopwatch.StartNew();

            var totalCount = await query.CountAsync(cancellationToken);

            var projects = await query.OrderByDescending(pm => pm.AddedAtUtc).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(pm => new ProjectListItem(pm.Project.Id, pm.Project.TeamId, pm.Project.Name,
                pm.Project.Team.Name, pm.Project.Description, pm.Project.Status, pm.Project.StartDate, pm.Project.DueDate,
                pm.Project.OwnerUserId, pm.Project.Owner.DisplayName, pm.Project.CreatedBy, pm.Project.Creator.DisplayName,
                pm.ProjectRole, pm.Project.Members.Count(m => ProjectMemberStatuses.Occupied.Contains(m.Status)),
                pm.Project.Tasks.Count(t => t.DeletedAtUtc == null), pm.Project.CreatedAtUtc, pm.Project.UpdatedAtUtc))
                .ToListAsync(cancellationToken);
            stopwatch.Stop();

            Console.WriteLine($"///////////////GetMyProjects took: {stopwatch.ElapsedMilliseconds} ms");
            return new GetMyProjectsResponse(projects, totalCount, request.Page, request.PageSize);
            //Console.WriteLine(projects);
            //return null;
        }
    }
}