using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;

namespace TeamManager.Infrastructure.Services.SecurityServices
{
    public sealed class PermissionService(IUserRepository userRepository, ICacheService cacheService) : IPermissionService
    {
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

        public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken)
        {
            var permissions = await cacheService.GetOrCreateAsync(GetCacheKey(userId),
                ct => userRepository.GetPermissionCodesAsync(userId, ct), CacheExpiration, cancellationToken);

            return permissions?.Contains(permissionCode, StringComparer.OrdinalIgnoreCase) == true;
        }

        public Task InvalidateUserPermissionsAsync(Guid userId, CancellationToken cancellationToken)
        {
            return cacheService.RemoveAsync(GetCacheKey(userId), cancellationToken);
        }

        private static string GetCacheKey(Guid userId) => $"teammanager:permissions:user:{userId:N}";
    }
}