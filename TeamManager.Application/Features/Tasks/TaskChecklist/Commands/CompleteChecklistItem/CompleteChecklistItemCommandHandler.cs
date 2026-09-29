using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Commands.CompleteChecklistItem
{
    public sealed class CompleteChecklistItemCommandHandler(ICurrentUser currentUser, ITaskRepository taskRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<CompleteChecklistItemCommand>
    {
        public async Task Handle(CompleteChecklistItemCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var task = await taskRepository.GetByIdWithChecklistAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            task.CompleteChecklistItem(request.ChecklistItemId, currentUser.UserId.Value);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}