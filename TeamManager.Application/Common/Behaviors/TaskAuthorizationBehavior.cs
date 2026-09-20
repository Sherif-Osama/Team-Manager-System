using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Common.Behaviors
{
    public sealed class TaskAuthorizationBehavior<TRequest, TResponse>(ITaskRepository taskRepository, IProjectRepository projectRepository,
        ICurrentUser currentUser, ITeamRepository teamRepository) : IPipelineBehavior<TRequest, TResponse> where TRequest : ITaskScopedRequest
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

            if (task is null)
                throw new TaskNotFoundException(request.TaskId);

            var hasProjectRole = await projectRepository.HasActiveRoleAsync(task.ProjectId, userId, request.RequiredProjectRoles, cancellationToken);

            if (hasProjectRole)
                return await next();

            var project = await projectRepository.GetByIdAsync(task.ProjectId, cancellationToken);

            if (project == null)
                throw new ProjectNotFoundException(task.ProjectId);

            var isTeamOwner = await teamRepository.HasActiveRoleAsync(project.TeamId, currentUser.UserId.Value, [TeamRole.Owner],
                cancellationToken);

            if (!isTeamOwner)
                throw new ForbiddenException("You do not have permission to perform this action on this project.");

            return await next();
        }
    }
}