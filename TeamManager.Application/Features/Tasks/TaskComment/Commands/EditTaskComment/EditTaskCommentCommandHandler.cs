using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.EditTaskComment
{
    public sealed class EditTaskCommentCommandHandler(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<EditTaskCommentCommand>
    {
        public async Task Handle(EditTaskCommentCommand request, CancellationToken cancellationToken)
        {
            var task = await taskRepository.GetByIdWithCommentsAndMentionsAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            task.EditComment(request.CommentId, request.Content, request.MentionedUserIds);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}