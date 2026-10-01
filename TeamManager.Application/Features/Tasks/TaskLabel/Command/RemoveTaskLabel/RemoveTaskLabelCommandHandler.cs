using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskLabel.Command.RemoveTaskLabel
{
    public sealed class RemoveTaskLabelCommandHandler(ITaskRepository taskRepository, IUnitOfWork unitOfWork) : IRequestHandler<RemoveTaskLabelCommand>
    {
        public async Task Handle(RemoveTaskLabelCommand request, CancellationToken cancellationToken)
        {
            var task = await taskRepository.GetByIdWithLabelsAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            task.RemoveLabel(request.LabelId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}