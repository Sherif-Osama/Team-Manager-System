using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.TaskExceptions;

namespace TeamManager.Application.Common.Behaviors
{
    public sealed class TaskAuthorizationBehavior<TRequest, TResponse>(ITaskRepository taskRepository, ICurrentUser currentUser)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : ITaskScopedRequest
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var authInfo = await taskRepository.GetAuthorizationInfoAsync(request.TaskId, currentUser.UserId.Value,
                request.RequiredProjectRoles, cancellationToken);

            if (authInfo is null)
                throw new TaskNotFoundException(request.TaskId);

            if (!authInfo.HasProjectRole && !authInfo.IsTeamOwner)
                throw new ForbiddenException("You do not have permission to perform this action on this project.");

            return await next();
        }
    }
}