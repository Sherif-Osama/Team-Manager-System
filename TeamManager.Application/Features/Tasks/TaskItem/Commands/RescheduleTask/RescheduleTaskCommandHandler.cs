using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.RescheduleTask
{
    public sealed class RescheduleTaskCommandHandler(ITaskRepository taskRepository, IProjectRepository projectRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<RescheduleTaskCommand>
    {
        public async Task Handle(RescheduleTaskCommand request, CancellationToken cancellationToken)
        {
            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var task = await taskRepository.GetByIdAsync(request.TaskId, ct);

                if (task is null)
                    throw new TaskNotFoundException(request.TaskId);

                var project = await projectRepository.GetByIdAsync(task.ProjectId, ct);

                if (project is null)
                    throw new ProjectNotFoundException(task.ProjectId);

                if (request.DueDate.HasValue)
                {
                    var violating = await taskRepository.GetDependentsViolatingDueDateAsync(task.Id, request.DueDate.Value, ct);
                    if (violating.Count > 0)
                        throw new TaskDueDateViolatesDependentTasksException(task.Id, violating);
                }

                task.Reschedule(request.StartDate, request.DueDate, project.StartDate, project.DueDate);
            }, cancellationToken);
        }
    }
}