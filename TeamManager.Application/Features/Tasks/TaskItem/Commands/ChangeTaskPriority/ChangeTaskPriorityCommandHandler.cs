using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskPriority
{
    public sealed class ChangeTaskPriorityCommandHandler(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<ChangeTaskPriorityCommand>
    {
        public async Task Handle(ChangeTaskPriorityCommand request, CancellationToken cancellationToken)
        {
            var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            task.ChangePriority(request.Priority);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}