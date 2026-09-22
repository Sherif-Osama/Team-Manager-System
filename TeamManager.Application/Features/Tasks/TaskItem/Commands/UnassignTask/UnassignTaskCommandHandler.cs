using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.UnassignTask
{
    public sealed class UnassignTaskCommandHandler(ITaskRepository taskRepository, IUnitOfWork unitOfWork) : IRequestHandler<UnassignTaskCommand>
    {
        public async Task Handle(UnassignTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            task.Unassign();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}