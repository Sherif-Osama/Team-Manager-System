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

            var dependency = task.Dependencies.SingleOrDefault();

            if (dependency is null)
                throw new TaskDependencyNotFoundException(request.TaskId, request.DependencyId);

            taskRepository.RemoveDependency(dependency);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}