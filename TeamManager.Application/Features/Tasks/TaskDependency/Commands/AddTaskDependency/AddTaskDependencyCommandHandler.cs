using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.TaskExceptions;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Application.Features.Tasks.TaskDependency.Commands.AddTaskDependency
{
    public sealed class AddTaskDependencyCommandHandler(ICurrentUser currentUser, ITaskRepository taskRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<AddTaskDependencyCommand, long>
    {
        public async Task<long> Handle(AddTaskDependencyCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || !currentUser.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            long dependencId = default;

            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var task = await taskRepository.GetByIdAsync(request.TaskId, ct);

                if (task is null)
                    throw new TaskNotFoundException(request.TaskId);

                var dependsOnTask = await taskRepository.GetByIdAsync(request.DependsOnTaskId, ct);

                if (dependsOnTask is null)
                    throw new TaskNotFoundException(request.DependsOnTaskId);

                if (task.ProjectId != dependsOnTask.ProjectId)
                    throw new DomainException("A task can only depend on another task in the same project.");

                var wouldCreateCycle = await taskRepository.WouldCreateDependencyCycleAsync(task.ProjectId,
                    request.TaskId, request.DependsOnTaskId, ct);

                if (wouldCreateCycle)
                    throw new DomainException("Adding this dependency would create a circular dependency.");

                var dependency = task.AddDependency(request.DependsOnTaskId, userId);

                dependencId = dependency.Id;

            }, cancellationToken);

            return dependencId;
        }
    }
}