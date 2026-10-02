using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskComment.Commands.AddTaskComment
{
    public sealed class AddTaskCommentCommandHandler(ITaskRepository taskRepository, ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : IRequestHandler<AddTaskCommentCommand>
    {
        public async Task Handle(AddTaskCommentCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            task.AddComment(currentUser.UserId.Value, request.Content);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}