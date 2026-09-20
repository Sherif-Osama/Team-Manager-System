using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskStatus
{
    public sealed record ChangeTaskStatusCommand(long TaskId, TaskItemStatus Status) : IRequest, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member];
    }
}