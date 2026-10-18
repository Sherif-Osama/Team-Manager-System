using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.Caching;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
namespace TeamManager.Application.Common.Behaviors
{
    public sealed class ProjectAuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser,
        IProjectRepository projectRepository, ICacheService cacheService)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : IProjectScopedRequest
    {
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue || !currentUser.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var membership = await cacheService.GetOrCreateAsync(GetCacheKey(request.ProjectId, userId),
                ct => projectRepository.GetMembershipInfoAsync(request.ProjectId, userId, ct),
                CacheExpiration,
                [CacheTags.ProjectAuthorization(request.ProjectId), CacheTags.UserAuthorization(userId)],
                cancellationToken);

            if (membership is null)
                throw new ProjectNotFoundException(request.ProjectId);

            var hasProjectRole = membership.Role is not null && request.RequiredRoles.Contains(membership.Role.Value);

            if (!hasProjectRole && !membership.IsTeamOwner)
                throw new ForbiddenException("You do not have permission to perform this action on this project.");

            return await next();
        }

        private static string GetCacheKey(Guid projectId, Guid userId) =>
            $"teammanager:auth:project:{projectId:N}:user:{userId:N}";
    }
}
