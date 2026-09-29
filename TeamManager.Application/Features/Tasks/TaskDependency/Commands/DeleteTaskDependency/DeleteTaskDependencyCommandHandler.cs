using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Features.Tasks.TaskDependency.Commands.DeleteTaskDependency
{
    public sealed class DeleteTaskDependencyCommandHandler(ITaskRepository taskRepository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteTaskDependencyCommand>
    {
        public async Task Handle(DeleteTaskDependencyCommand request, CancellationToken cancellationToken)
        {
            var task = await taskRepository.GetByIdWithDependencyAsync(request.TaskId, request.DependencyId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            var dependency = task.RemoveDependency(request.DependencyId);

            taskRepository.RemoveDependency(dependency);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}