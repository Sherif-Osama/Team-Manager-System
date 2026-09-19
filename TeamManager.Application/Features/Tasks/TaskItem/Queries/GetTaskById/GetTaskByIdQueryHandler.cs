using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Tasks.TaskItem.Queries.GetTaskById
{
    public sealed class GetTaskByIdQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetTaskByIdQuery, GetTaskByIdResponse>
    {
        public async Task<GetTaskByIdResponse> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
        {
            var task = await context.Tasks
                .AsNoTracking().Where(x => x.Id == request.TaskId && x.DeletedAtUtc == null)
              .Select(x => new GetTaskByIdResponse(x.Id, x.ProjectId, x.Project.Name, x.Title, x.Description, x.Status, x.Priority,
              x.CreatedBy, x.Creator.DisplayName, x.AssigneeUserId, x.Assignee != null ? x.Assignee.DisplayName : null, x.StartDate,
              x.DueDate, x.CompletedAtUtc, x.CreatedAtUtc, x.UpdatedAtUtc, x.Dependencies.Count, x.Labels.Count,
              x.ChecklistItems.Count, x.Attachments.Count, x.Comments.Count)).FirstOrDefaultAsync(cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            return task;
        }
    }
}