using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.Caching;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;

namespace TeamManager.Application.Common.Behaviors
{
    public sealed class TeamAuthorizationBehavior<TRequest, TResponse>(ITeamRepository teamRepository,
        ICurrentUser currentUser, ICacheService cacheService) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ITeamScopedRequest
    {
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("You do not have permission to perform this action on this team.");

            var userId = currentUser.UserId.Value;

            var role = await cacheService.GetOrCreateAsync(GetCacheKey(request.TeamId, userId),
                ct => teamRepository.GetActiveRoleAsync(request.TeamId, userId, ct),
                CacheExpiration,
                [CacheTags.TeamAuthorization(request.TeamId), CacheTags.UserAuthorization(userId)],
                cancellationToken);

            if (role is null || !request.RequiredRoles.Contains(role.Value))
                throw new ForbiddenException("You do not have permission to perform this action on this team.");

            return await next();
        }

        private static string GetCacheKey(Guid teamId, Guid userId) =>
            $"teammanager:auth:team:{teamId:N}:user:{userId:N}";
    }
}
