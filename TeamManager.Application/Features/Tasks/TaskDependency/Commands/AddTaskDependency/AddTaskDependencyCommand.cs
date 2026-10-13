using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskDependency.Commands.AddTaskDependency
{
    public sealed record AddTaskDependencyCommand(long TaskId, long DependsOnTaskId) : IRequest<long>, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin];
    }
}