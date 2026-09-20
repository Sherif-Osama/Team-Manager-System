using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.CreateTask
{
    public sealed record CreateTaskCommand(Guid ProjectId, string Title, string? Description = null, Guid? AssigneeUserId = null,
        DateOnly? StartDate = null, DateOnly? DueDate = null, TaskPriority Priority = TaskPriority.Medium) : IRequest<long>, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner, ProjectRole.Admin];
    }
}