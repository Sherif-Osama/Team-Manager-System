using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetMyTasks
{
    public sealed class GetMyTasksQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
        : IRequestHandler<GetMyTasksQuery, GetMyTasksResponse>
    {
        public async Task<GetMyTasksResponse> Handle(GetMyTasksQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var query = context.Tasks.AsNoTracking().Where(x => x.AssigneeUserId == userId && x.DeletedAtUtc == null
            && x.Project.DeletedAtUtc == null);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x => x.Title.Contains(search) || (x.Description != null && x.Description.Contains(search)) ||
                    x.Project.Name.Contains(search));
            }

            if (request.Status.HasValue)
                query = query.Where(x => x.Status == request.Status.Value);


            if (request.Priority.HasValue)
                query = query.Where(x => x.Priority == request.Priority.Value);


            var totalCount = await query.CountAsync(cancellationToken);

            var tasks = await query.OrderBy(x => x.DueDate == null).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(x => new GetMyTasksItem(x.Id, x.ProjectId, x.AssigneeUserId!.Value,
                x.Assignee!.DisplayName, x.Project.Name, x.Title, x.Description, x.Status, x.Priority, x.CreatedBy,
                x.Creator.DisplayName, x.StartDate, x.DueDate, x.CompletedAtUtc, x.CreatedAtUtc, x.UpdatedAtUtc))
                .ToListAsync(cancellationToken);

            return new GetMyTasksResponse(tasks, totalCount, request.Page, request.PageSize);
        }
    }
}