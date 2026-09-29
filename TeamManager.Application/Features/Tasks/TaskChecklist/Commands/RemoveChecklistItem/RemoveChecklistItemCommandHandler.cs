using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Commands.RemoveChecklistItem
{
    public sealed class RemoveChecklistItemCommandHandler(ICurrentUser currentUser, ITaskRepository taskRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<RemoveChecklistItemCommand>
    {
        public async Task Handle(RemoveChecklistItemCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var task = await taskRepository.GetByIdWithChecklistAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            task.RemoveChecklistItem(request.ChecklistItemId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}