using TeamManager.Application.Abstractions.Caching;

namespace TeamManager.Infrastructure.Services.Caching
{
    public sealed class PostCommitCacheInvalidationDispatcher(ICacheService cacheService) : IPostCommitCacheInvalidationDispatcher
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
            foreach (var tag in _pendingTags)
            {
                await cacheService.RemoveByTagAsync(tag, cancellationToken);

                _pendingTags.Remove(tag);
            }
        }

        public void Discard()
        {
            _pendingTags.Clear();
        }
    }
}