using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.TaskDependency
{
    public sealed record AddTaskDependencyCommand(long TaskId, long DependsOnTaskId) : IRequest<long>, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin];
    }
}