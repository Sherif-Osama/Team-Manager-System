using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Tasks.TaskItem.Commands.ChangeTaskStatus
{
    public sealed class ChangeTaskStatusCommandHandler(ICurrentUser currentUser, ITaskRepository taskRepository,
        IProjectRepository projectRepository, IUnitOfWork unitOfWork) : IRequestHandler<ChangeTaskStatusCommand>
    {
        public async Task Handle(ChangeTaskStatusCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            var userId = currentUser.UserId.Value;

            var isAdminOrOwner = await projectRepository.HasActiveRoleAsync(task.ProjectId, userId,
                [ProjectRole.Owner, ProjectRole.Admin], cancellationToken);

            var isAssignee = task.AssigneeUserId == userId;

            if (!isAdminOrOwner && !isAssignee)
                throw new ForbiddenException("You do not have permission to change this task status.");

            // Assignees can only move tasks through their normal workflow states,
            // while Owners and Admins are allowed to perform any valid domain transition.
            if (!isAdminOrOwner && !CanAssigneeChangeStatus(task.Status, request.Status))
                throw new ForbiddenException("You do not have permission to perform this status transition.");

            task.ChangeStatus(request.Status);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static bool CanAssigneeChangeStatus(TaskItemStatus currentStatus, TaskItemStatus newStatus)
        {
            return currentStatus switch
            {
                TaskItemStatus.Todo => newStatus == TaskItemStatus.InProgress,

                TaskItemStatus.InProgress => newStatus == TaskItemStatus.InReview,

                TaskItemStatus.InReview => newStatus == TaskItemStatus.InProgress,

                _ => false
            };
        }
    }
}