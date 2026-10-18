using Moq;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Infrastructure.Services.SecurityServices;

namespace TeamManager.Tests.UnitTest.Infrastructure.SecurityServices;

public sealed class PermissionServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly PermissionService _sut;

    public PermissionServiceTests()
    {
        _sut = new(_userRepository.Object, _cacheService.Object);
        _cacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyCollection<string>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<IReadOnlyCollection<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string key, Func<CancellationToken, Task<IReadOnlyCollection<string>>> factory, TimeSpan expiration,
                IReadOnlyCollection<string>? tags, CancellationToken ct) => factory(ct));
    }

    private void SetupPermissionCodes(params string[] codes) =>
        _userRepository.Setup(x => x.GetPermissionCodesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(codes);

    [Fact]
    public async Task HasPermissionAsync_WhenCodePresent_ReturnsTrue()
    {
        SetupPermissionCodes("system.manage_roles");

        var result = await _sut.HasPermissionAsync(_userId, "system.manage_roles", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task HasPermissionAsync_WhenCodeMissing_ReturnsFalse()
    {
        SetupPermissionCodes("system.manage_roles");

        var result = await _sut.HasPermissionAsync(_userId, "system.manage_users", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserHasNoPermissions_ReturnsFalse()
    {
        SetupPermissionCodes();

        var result = await _sut.HasPermissionAsync(_userId, "system.manage_roles", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HasPermissionAsync_IsCaseInsensitive()
    {
        SetupPermissionCodes("system.manage_roles");

        var result = await _sut.HasPermissionAsync(_userId, "SYSTEM.MANAGE_ROLES", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task HasPermissionAsync_UsesUserScopedKeyAndBothTags()
    {
        SetupPermissionCodes("system.manage_roles");

        await _sut.HasPermissionAsync(_userId, "system.manage_roles", CancellationToken.None);

        _cacheService.Verify(x => x.GetOrCreateAsync(
            $"teammanager:permissions:user:{_userId:N}:codes",
            It.IsAny<Func<CancellationToken, Task<IReadOnlyCollection<string>>>>(),
            It.IsAny<TimeSpan>(),
            It.Is<IReadOnlyCollection<string>?>(tags => tags != null
                && tags.Contains($"teammanager:permissions:user:{_userId:N}")
                && tags.Contains("teammanager:permissions:all")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateUserPermissionsAsync_RemovesUserTag()
    {
        await _sut.InvalidateUserPermissionsAsync(_userId, CancellationToken.None);

        _cacheService.Verify(x => x.RemoveByTagAsync($"teammanager:permissions:user:{_userId:N}",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
