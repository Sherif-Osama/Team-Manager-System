using Microsoft.Extensions.Caching.Memory;
using TeamManager.Application.Abstractions.Caching;

namespace TeamManager.Infrastructure.Services.Caching
{
    public sealed class MemoryCacheService(IMemoryCache memoryCache) : ICacheService
    {
        public async Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan expiration,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be empty.", nameof(key));

            ArgumentNullException.ThrowIfNull(factory);

            if (expiration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(expiration));

            cancellationToken.ThrowIfCancellationRequested();

            return await memoryCache.GetOrCreateAsync(key, async entry =>
                    {
                        entry.AbsoluteExpirationRelativeToNow = expiration;

                        return await factory(cancellationToken);
                    });
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be empty.", nameof(key));

            cancellationToken.ThrowIfCancellationRequested();

            memoryCache.Remove(key);

            return Task.CompletedTask;
        }
    }
}