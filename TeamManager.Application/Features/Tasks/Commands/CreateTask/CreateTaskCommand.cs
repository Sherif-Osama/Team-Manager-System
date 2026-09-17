using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.Commands.CreateTask
{
    public sealed record CreateTaskCommand(Guid ProjectId, string Title, string? Description, Guid? AssigneeUserId, DateOnly? StartDate, DateOnly? DueDate,
        TaskPriority Priority) : IRequest<Guid>, IProjectScopedRequest
    {
        public ProjectRole[] RequiredRoles => [ProjectRole.Owner, ProjectRole.Admin];
    }
}