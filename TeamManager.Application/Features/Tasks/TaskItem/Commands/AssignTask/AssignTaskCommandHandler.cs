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
            var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            var isActiveMember = await projectRepository.IsActiveMemberAsync(task.ProjectId, request.UserId, cancellationToken);

            if (!isActiveMember)
                throw new UserNotMemberOfProjectException(task.ProjectId, request.UserId);

            task.Assign(request.UserId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}