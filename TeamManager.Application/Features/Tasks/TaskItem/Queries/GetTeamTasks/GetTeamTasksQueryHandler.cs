using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTeamTasks
{
    public sealed class GetTeamTasksQueryHandler(IApplicationDbContext context, ITeamRepository teamRepository,
        ICurrentUser currentUser) : IRequestHandler<GetTeamTasksQuery, GetTeamTasksResponse>
    {
        public async Task<GetTeamTasksResponse> Handle(GetTeamTasksQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || !currentUser.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var query = context.Tasks.AsNoTracking().Where(x => x.Project.TeamId == request.TeamId && x.DeletedAtUtc == null &&
            x.Project.DeletedAtUtc == null);

            var isTeamOwner = await teamRepository.HasActiveRoleAsync(request.TeamId, userId, [TeamRole.Owner], cancellationToken);

            if (!isTeamOwner)
                query = query.Where(x => x.Project.Members.Any(pm => pm.UserId == userId && pm.Status == ProjectMemberStatus.Active
                && pm.User.IsActive));

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();

                query = query.Where(x => x.Title.Contains(search) || (x.Description != null && x.Description.Contains(search))
                || x.Project.Name.Contains(search));
            }

            if (request.ProjectId.HasValue)
                query = query.Where(x => x.ProjectId == request.ProjectId.Value);

            if (request.Status.HasValue)
                query = query.Where(x => x.Status == request.Status.Value);

            if (request.Priority.HasValue)
                query = query.Where(x => x.Priority == request.Priority.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var tasks = await query.OrderBy(t => t.DueDate == null).Skip((request.Page - 1) * request.PageSize).
                Take(request.PageSize).Select(x => new GetTeamTasksItem(x.Id, x.ProjectId, x.Project.Name, x.AssigneeUserId,
                x.Assignee != null ? x.Assignee.DisplayName : null, x.Title, x.Description, x.Status, x.Priority,
                x.CreatedBy, x.Creator.DisplayName, x.StartDate, x.DueDate, x.CompletedAtUtc, x.CreatedAtUtc,
                x.UpdatedAtUtc, x.Labels.Select(label => new GetTeamTaskLabelResponse(label.LabelId, label.Label.Name,
                label.Label.ColorHex)).ToList())).ToListAsync(cancellationToken);

            return new GetTeamTasksResponse(tasks, totalCount, request.Page, request.PageSize);

        }
    }
}