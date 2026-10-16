using Microsoft.Extensions.Caching.Hybrid;
using TeamManager.Application.Abstractions.Caching;

namespace TeamManager.Infrastructure.Services.Caching
{
    public sealed class HybridCacheService(HybridCache hybridCache) : ICacheService
    {
        public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan expiration,
            IReadOnlyCollection<string>? tags = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be empty.", nameof(key));

            ArgumentNullException.ThrowIfNull(factory);

            if (expiration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(expiration));

            if (tags?.Any(tag => string.IsNullOrWhiteSpace(tag) || tag == "*") == true)
                throw new ArgumentException("Cache tags cannot be empty or '*'.", nameof(tags));

            var options = new HybridCacheEntryOptions
            {
                Expiration = expiration,
                LocalCacheExpiration = expiration
            };

            return await hybridCache.GetOrCreateAsync(key, async ct => await factory(ct), options, tags, cancellationToken);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be empty.", nameof(key));

            await hybridCache.RemoveAsync(key, cancellationToken);
        }

        public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tag) || tag == "*")
                throw new ArgumentException("A valid cache tag is required.", nameof(tag));

            await hybridCache.RemoveByTagAsync(tag, cancellationToken);
        }
    }
}