using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.AssignTask
{
    public sealed class AssignTaskCommandHandler(ITaskRepository taskRepository, IProjectRepository projectRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<AssignTaskCommand>
    {
        public async Task Handle(AssignTaskCommand request, CancellationToken cancellationToken)
        {
            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var task = await taskRepository.GetByIdAsync(request.TaskId, ct);

                if (task is null)
                    throw new TaskNotFoundException(request.TaskId);

                var isActiveMember = await projectRepository.IsActiveMemberAsync(task.ProjectId, request.UserId, ct);

                if (!isActiveMember)
                    throw new UserNotMemberOfProjectException(task.ProjectId, request.UserId);

                task.Assign(request.UserId);

            }, cancellationToken);
        }
    }
}