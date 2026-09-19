using MediatR;
using TeamManager.Application.Abstractions;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Common.Behaviors
{
    public sealed class TaskAuthorizationBehavior<TRequest, TResponse>(ITaskRepository taskRepository, IProjectRepository projectRepository,
        ICurrentUser currentUser) : IPipelineBehavior<TRequest, TResponse> where TRequest : ITaskScopedRequest
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

            if (!hasProjectRole)
                throw new ForbiddenException("You do not have permission to access this task.");

            return await next();
        }
    }
}