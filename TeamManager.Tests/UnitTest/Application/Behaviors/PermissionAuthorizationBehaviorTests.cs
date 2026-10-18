using MediatR;
using Moq;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Behaviors;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;

namespace TeamManager.Tests.UnitTest.Application.Behaviors;

public sealed class PermissionAuthorizationBehaviorTests
{
    private const string PermissionCode = "system.manage_roles";
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IPermissionService> _permissionService = new();
    private readonly PermissionAuthorizationBehavior<TestPermissionRequest, string> _sut;

    public PermissionAuthorizationBehaviorTests()
    {
        _sut = new(_currentUser.Object, _permissionService.Object);
        _currentUser.SetupGet(x => x.UserId).Returns(_userId);
    }

    private void SetupPermission(bool hasPermission) =>
    _permissionService.Setup(x => x.HasPermissionAsync(_userId, PermissionCode, It.IsAny<CancellationToken>())).ReturnsAsync(hasPermission);

    private sealed record TestPermissionRequest : IRequest<string>, IRequiresPermission
    {
        public string PermissionCode => PermissionAuthorizationBehaviorTests.PermissionCode;
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_Throws401AndDoesNotCallNext()
    {
        _currentUser.SetupGet(x => x.UserId).Returns((Guid?)null);

        var nextCalled = false;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(new TestPermissionRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
        _permissionService.Verify(x => x.HasPermissionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotHavePermission_Throws403AndDoesNotCallNext()
    {
        SetupPermission(false);
        var nextCalled = false;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.Handle(new TestPermissionRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_Throws403AndDoesNotCallNext()
    {
        // The repository's effective permission query excludes inactive users.
        SetupPermission(false);
        var nextCalled = false;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.Handle(new TestPermissionRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenPermissionExists_CallsNextAndReturnsItsResult()
    {
        SetupPermission(true);
        var nextCalled = false;

        var result = await _sut.Handle(new TestPermissionRequest(), _ =>
        {
            nextCalled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("allowed", result);
    }

    [Fact]
    public async Task Handle_SendsTheRequestPermissionCodeToRepository()
    {
        SetupPermission(true);

        await _sut.Handle(new TestPermissionRequest(), _ => Task.FromResult("allowed"), CancellationToken.None);

        _permissionService.Verify(x => x.HasPermissionAsync(_userId, PermissionCode, It.IsAny<CancellationToken>()), Times.Once);
    }
}