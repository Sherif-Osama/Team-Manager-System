namespace TeamManager.Application.Abstractions.Caching
{
    public interface ICacheableQuery
    {
        string CacheKey { get; }
        TimeSpan CacheExpiration { get; }
        IReadOnlyCollection<string>? Tags { get; }
    }
}