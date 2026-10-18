using Microsoft.Extensions.Logging;
using Moq;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Infrastructure.Services.Caching;

namespace TeamManager.Tests.UnitTest.Infrastructure.Caching;

public sealed class PostCommitCacheInvalidationDispatcherTests
{
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogger<PostCommitCacheInvalidationDispatcher>> _logger = new();
    private readonly PostCommitCacheInvalidationDispatcher _sut;

    public PostCommitCacheInvalidationDispatcherTests()
    {
        _sut = new(_cacheService.Object, _logger.Object);
    }

    [Fact]
    public async Task DispatchAsync_RemovesEachEnqueuedTag()
    {
        _sut.EnqueueTag("tag-1");
        _sut.EnqueueTag("tag-2");

        await _sut.DispatchAsync(CancellationToken.None);

        _cacheService.Verify(x => x.RemoveByTagAsync("tag-1", It.IsAny<CancellationToken>()), Times.Once);
        _cacheService.Verify(x => x.RemoveByTagAsync("tag-2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_AfterDispatch_PendingTagsAreCleared()
    {
        _sut.EnqueueTag("tag-1");

        await _sut.DispatchAsync(CancellationToken.None);
        await _sut.DispatchAsync(CancellationToken.None);

        _cacheService.Verify(x => x.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueTag_DeduplicatesCaseInsensitively()
    {
        _sut.EnqueueTag("Tag-1");
        _sut.EnqueueTag("tag-1");

        await _sut.DispatchAsync(CancellationToken.None);

        _cacheService.Verify(x => x.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void EnqueueTag_WithBlankTag_Throws()
    {
        Assert.Throws<ArgumentException>(() => _sut.EnqueueTag(""));
    }

    [Fact]
    public void EnqueueTag_WithWildcardTag_Throws()
    {
        Assert.Throws<ArgumentException>(() => _sut.EnqueueTag("*"));
    }

    [Fact]
    public async Task Discard_ClearsTagsWithoutDispatching()
    {
        _sut.EnqueueTag("tag-1");

        _sut.Discard();
        await _sut.DispatchAsync(CancellationToken.None);

        _cacheService.Verify(x => x.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_WhenRemovalFails_LogsAndContinuesWithRemainingTags()
    {
        _cacheService.Setup(x => x.RemoveByTagAsync("failing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        _sut.EnqueueTag("failing");
        _sut.EnqueueTag("working");

        await _sut.DispatchAsync(CancellationToken.None);

        _cacheService.Verify(x => x.RemoveByTagAsync("working", It.IsAny<CancellationToken>()), Times.Once);
        _logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
