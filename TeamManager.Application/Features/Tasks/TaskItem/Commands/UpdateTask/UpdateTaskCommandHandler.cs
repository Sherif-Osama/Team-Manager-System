using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.UpdateTask
{
    public sealed class UpdateTaskCommandHandler(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateTaskCommand>
    {
        public async Task Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            task.Rename(request.Title);
            task.UpdateDescription(request.Description);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}