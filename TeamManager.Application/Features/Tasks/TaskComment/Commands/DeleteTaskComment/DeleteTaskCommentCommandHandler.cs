using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.DeleteTaskComment
{
    public sealed class DeleteTaskCommentCommandHandler(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteTaskCommentCommand>
    {
        public async Task Handle(DeleteTaskCommentCommand request, CancellationToken cancellationToken)
        {

            var task = await taskRepository.GetByIdWithCommentsAndMentionsAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            task.DeleteComment(request.CommentId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}