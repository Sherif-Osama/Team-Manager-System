using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Application.Common.Exceptions;
namespace TeamManager.Application.Common.Behaviors
{
    public class ProjectAuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser, IProjectRepository projectRepository)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : IProjectScopedRequest
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || !currentUser.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var authInfo = await projectRepository.GetAuthorizationInfoAsync(request.ProjectId, currentUser.UserId.Value,
                request.RequiredRoles, cancellationToken);

            if (authInfo is null)
                throw new ProjectNotFoundException(request.ProjectId);

            if (!authInfo.HasProjectRole && !authInfo.IsTeamOwner)
                throw new ForbiddenException("You do not have permission to perform this action on this project.");

            return await next();
        }
    }
}