namespace TeamManager.Application.Abstractions.Caching
{
    public interface ICacheService
    {
        Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan expiration,
            IReadOnlyCollection<string>? tags = null, CancellationToken cancellationToken = default);

        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
    }
}