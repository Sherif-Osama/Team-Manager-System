using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.TaskExceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.DeleteTaskComment
{
    public sealed class DeleteTaskCommentCommandHandler(ITaskRepository taskRepository, IUnitOfWork unitOfWork,
        ICurrentUser currentUser, IProjectRepository projectRepository) : IRequestHandler<DeleteTaskCommentCommand>
    {
        public async Task Handle(DeleteTaskCommentCommand request, CancellationToken cancellationToken)
        {
            var task = await taskRepository.GetByIdWithCommentsAndMentionsAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            var comment = task.Comments.FirstOrDefault(x => x.Id == request.CommentId)
              ?? throw new CommentNotFoundException();

            var userId = currentUser.UserId!.Value;

            if (comment.AuthorUserId != userId && !await projectRepository.HasActiveRoleAsync(task.ProjectId, userId, [ProjectRole.Owner, ProjectRole.Admin], cancellationToken))
                throw new ForbiddenException("You can only delete your own comments.");

            task.DeleteComment(request.CommentId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}