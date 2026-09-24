using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.CreateTask
{
    public sealed class CreateTaskCommandHandler(ICurrentUser currentUser, IProjectRepository projectRepository,
        ITaskRepository taskRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateTaskCommand, long>
    {
        public async Task<long> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || !currentUser.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            long taskId = default;

            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var project = await projectRepository.GetByIdAsync(request.ProjectId, ct);

                if (project is null)
                    throw new ProjectNotFoundException(request.ProjectId);

                if (project.Status != ProjectStatus.Active)
                    throw new ProjectNotActiveException(project.Id);

                if (request.AssigneeUserId.HasValue)
                {
                    var isActiveMember = await projectRepository.IsActiveMemberAsync(project.Id, request.AssigneeUserId.Value, ct);

                    if (!isActiveMember) throw new UserNotMemberOfProjectException(request.AssigneeUserId.Value, project.Id);
                }

                var task = new Domain.Entities.TaskItem(project.Id, request.Title, currentUser.UserId.Value, request.Priority,
                    request.Description, request.AssigneeUserId, request.StartDate, request.DueDate, project.StartDate, project.DueDate);

                await taskRepository.AddAsync(task, ct);
                taskId = task.Id;

            }, cancellationToken);

            return taskId;
        }
    }
}