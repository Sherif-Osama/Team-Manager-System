using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.EditTaskComment
{
    public sealed class EditTaskCommentCommandHandler(ITaskRepository taskRepository, IProjectRepository projectRepository,
       ICurrentUser currentUser, IUnitOfWork unitOfWork) : IRequestHandler<EditTaskCommentCommand>
    {
        public async Task Handle(EditTaskCommentCommand request, CancellationToken cancellationToken)
        {
            var task = await taskRepository.GetByIdWithCommentsAndMentionsAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            var comment = task.Comments.FirstOrDefault(x => x.Id == request.CommentId);

            if (comment is null)
                throw new CommentNotFoundException();

            if (comment.AuthorUserId != currentUser.UserId!.Value)
                throw new ForbiddenException("Only the author can edit this comment.");

            var mentionedUserIds = request.MentionedUserIds?.Distinct().Where(id => id != comment.AuthorUserId).ToArray();

            if (mentionedUserIds is not null)
            {
                foreach (var userId in mentionedUserIds)
                {
                    var isActiveMember = await projectRepository.IsActiveMemberAsync(task.ProjectId, userId, cancellationToken);

                    if (!isActiveMember)
                        throw new UserNotMemberOfProjectException(userId, task.ProjectId);
                }
            }

            task.EditComment(request.CommentId, request.Content, request.MentionedUserIds);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}