namespace TeamManager.Application.Abstractions.Caching
{
    public interface IPostCommitCacheInvalidationDispatcher
    {
        void EnqueueTag(string tag);

        Task DispatchAsync(CancellationToken cancellationToken);

        void Discard();
    }
}