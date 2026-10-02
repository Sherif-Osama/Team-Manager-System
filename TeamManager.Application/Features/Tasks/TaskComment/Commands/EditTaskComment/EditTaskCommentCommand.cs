using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.EditTaskComment
{
    public sealed record EditTaskCommentCommand(long TaskId, long CommentId, string Content) : IRequest, ITaskScopedRequest
    {
        public ProjectRole[] RequiredProjectRoles => [ProjectRole.Owner, ProjectRole.Admin, ProjectRole.Member];
    }
}