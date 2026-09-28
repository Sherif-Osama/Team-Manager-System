using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskChecklist.Commands.AddChecklistItem
{
    public sealed class AddChecklistItemCommandHandler(ICurrentUser currentUser, ITaskRepository taskRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<AddChecklistItemCommand, long>
    {
        public async Task<long> Handle(AddChecklistItemCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var task = await taskRepository.GetByIdWithChecklistAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            var item = task.AddChecklistItem(request.Content);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return item.Id;
        }
    }
}