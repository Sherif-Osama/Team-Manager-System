using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Tasks.TaskDependency.Queries.GetTaskDependencies
{
    public sealed class GetTaskDependenciesQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetTaskDependenciesQuery, GetTaskDependenciesResponse>
    {
        public async Task<GetTaskDependenciesResponse> Handle(GetTaskDependenciesQuery request, CancellationToken cancellationToken)
        {
            var query = context.TaskDependencies.AsNoTracking()
                .Where(td => td.TaskId == request.TaskId && td.DependsOnTask.DeletedAtUtc == null);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query.Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(td => new TaskDependencyItem(td.Id, td.TaskId, td.DependsOnTaskId,
                td.DependsOnTask.Title, td.DependsOnTask.Status, td.DependsOnTask.Priority, td.DependsOnTask.DueDate, td.CreatedBy,
                td.CreatedByUser.DisplayName, td.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetTaskDependenciesResponse(items, totalCount, request.Page, request.PageSize);
        }
    }
}