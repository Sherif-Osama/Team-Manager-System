using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskLabel.Commands.AddTaskLabel
{
    public sealed record AddTaskLabelCommand(long TaskId, long LabelId) : IRequest, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin];
    }
}