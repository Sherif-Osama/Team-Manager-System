using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.UpdateTask
{
    public sealed record UpdateTaskCommand(long TaskId, string Title, string? Description) : IRequest, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner];
    }
}