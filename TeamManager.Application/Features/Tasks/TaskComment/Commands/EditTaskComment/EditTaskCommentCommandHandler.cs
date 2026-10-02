using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.EditTaskComment
{
    public sealed class EditTaskCommentCommandHandler(ITaskRepository taskRepository, ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : IRequestHandler<EditTaskCommentCommand>
    {
        public async Task Handle(EditTaskCommentCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var task = await taskRepository.GetByIdWithCommentsAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            task.EditComment(request.CommentId, currentUser.UserId.Value, request.Content);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}