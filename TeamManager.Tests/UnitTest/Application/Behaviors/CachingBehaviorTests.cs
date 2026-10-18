using MediatR;
using Moq;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Common.Behaviors;

namespace TeamManager.Tests.UnitTest.Application.Behaviors;

public sealed class CachingBehaviorTests
{
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly CachingBehavior<TestCacheableRequest, string> _sut;

    public CachingBehaviorTests()
    {
        _sut = new(_cacheService.Object);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedValueWithoutCallingNext()
    {
        var request = new TestCacheableRequest();
        _cacheService.Setup(x => x.GetOrCreateAsync(
                request.CacheKey,
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                request.CacheExpiration,
                request.Tags,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("cached-value");
        var nextCalled = false;

        var result = await _sut.Handle(request, _ =>
        {
            nextCalled = true;
            return Task.FromResult("fresh-value");
        }, CancellationToken.None);

        Assert.Equal("cached-value", result);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_CallsNextAndCachesItsResult()
    {
        var request = new TestCacheableRequest();
        _cacheService.Setup(x => x.GetOrCreateAsync(
                request.CacheKey,
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                request.CacheExpiration,
                request.Tags,
                It.IsAny<CancellationToken>()))
            .Returns((string key, Func<CancellationToken, Task<string>> factory, TimeSpan expiration,
                IReadOnlyCollection<string>? tags, CancellationToken ct) => factory(ct));
        var nextCalled = false;

        var result = await _sut.Handle(request, _ =>
        {
            nextCalled = true;
            return Task.FromResult("fresh-value");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("fresh-value", result);
    }

    [Fact]
    public async Task Handle_UsesRequestCacheKeyExpirationAndTags()
    {
        var request = new TestCacheableRequest("custom-key", TimeSpan.FromMinutes(3), ["tag-1", "tag-2"]);
        _cacheService.Setup(x => x.GetOrCreateAsync(
                request.CacheKey,
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                request.CacheExpiration,
                request.Tags,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("cached-value");

        await _sut.Handle(request, _ => Task.FromResult("fresh-value"), CancellationToken.None);

        _cacheService.Verify(x => x.GetOrCreateAsync(
            "custom-key",
            It.IsAny<Func<CancellationToken, Task<string>>>(),
            TimeSpan.FromMinutes(3),
            request.Tags,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed record TestCacheableRequest(string Key = "default-key",
        TimeSpan Expiration = default, IReadOnlyCollection<string>? TagList = null)
        : IRequest<string>, ICacheableQuery
    {
        public string CacheKey => Key;

        public TimeSpan CacheExpiration => Expiration == default ? TimeSpan.FromMinutes(5) : Expiration;

        public IReadOnlyCollection<string>? Tags => TagList;
    }
}
