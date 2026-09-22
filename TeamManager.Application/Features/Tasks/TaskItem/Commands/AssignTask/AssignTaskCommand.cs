using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;
namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.AssignTask
{
    public sealed record AssignTaskCommand(long TaskId, Guid UserId) : IRequest, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin];
    }
}