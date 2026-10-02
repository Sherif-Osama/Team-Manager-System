using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.AddTaskComment
{
    public sealed record AddTaskCommentCommand(long TaskId, string Content) : IRequest, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member];
    }
}