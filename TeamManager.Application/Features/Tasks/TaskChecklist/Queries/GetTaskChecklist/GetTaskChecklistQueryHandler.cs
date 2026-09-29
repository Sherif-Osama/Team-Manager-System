using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Queries.GetTaskChecklist
{
    public sealed class GetTaskChecklistQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetTaskChecklistQuery, IReadOnlyCollection<GetTaskChecklistResponse>>
    {
        public async Task<IReadOnlyCollection<GetTaskChecklistResponse>> Handle(GetTaskChecklistQuery request,
            CancellationToken cancellationToken)
        {
            var query = context.TaskChecklistItems.AsNoTracking().Where(x => x.TaskId == request.TaskId);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(x => x.Content.Contains(request.Search.Trim()));

            if (request.IsCompleted.HasValue)
                query = query.Where(x => x.IsCompleted == request.IsCompleted.Value);

            return await query.OrderBy(x => x.SortOrder)
                .Select(x => new GetTaskChecklistResponse(x.Id, x.TaskId, x.Content, x.IsCompleted,
                x.SortOrder, x.CompletedAtUtc, x.CompletedBy, x.CompletedByUser != null ? x.CompletedByUser.DisplayName : null,
                x.CreatedAtUtc)).ToListAsync(cancellationToken);
        }
    }
}