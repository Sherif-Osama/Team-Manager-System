using MediatR;
using TeamManager.Application.Abstractions;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.Exceptions;
namespace TeamManager.Application.Common.Behaviors
{
    internal class ProjectAuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser,
        IProjectRepository projectRepository) : IPipelineBehavior<TRequest, TResponse> where TRequest : IProjectScopedRequest
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || !currentUser.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var hasRequiredRole = await projectRepository.HasActiveRoleAsync(request.ProjectId, currentUser.UserId.Value,
                request.RequiredRoles, cancellationToken);

            if (!hasRequiredRole)
                throw new ForbiddenException("You do not have permission to perform this action on this project.");

            return await next();
        }
    }
}