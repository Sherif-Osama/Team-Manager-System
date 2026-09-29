using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Commands.ReopenChecklistItem
{
    public sealed class ReopenChecklistItemCommandHandler(ICurrentUser currentUser, ITaskRepository taskRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<ReopenChecklistItemCommand>
    {
        public async Task Handle(ReopenChecklistItemCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var task = await taskRepository.GetByIdWithChecklistAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            task.ReopenChecklistItem(request.ChecklistItemId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}