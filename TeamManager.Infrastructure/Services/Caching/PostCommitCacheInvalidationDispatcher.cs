using Microsoft.Extensions.Logging;
using TeamManager.Application.Abstractions.Caching;
namespace TeamManager.Infrastructure.Services.Caching
{
    public sealed class PostCommitCacheInvalidationDispatcher(ICacheService cacheService,
        ILogger<PostCommitCacheInvalidationDispatcher> logger) : IPostCommitCacheInvalidationDispatcher
    {
        private readonly HashSet<string> _pendingTags = new(StringComparer.OrdinalIgnoreCase);

        public void EnqueueTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || tag == "*")
            {
                throw new ArgumentException("A valid cache tag is required.", nameof(tag));
            }

            _pendingTags.Add(tag);
        }

        public async Task DispatchAsync(CancellationToken cancellationToken)
        {
            var tags = _pendingTags.ToArray();
            _pendingTags.Clear();

            foreach (var tag in tags)
            {
                try { await cacheService.RemoveByTagAsync(tag, cancellationToken); }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Cache invalidation failed for tag {Tag}", tag);
                }
            }
        }

        public void Discard()
        {
            _pendingTags.Clear();
        }
    }
}