using MediatR;
using Moq;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.Behaviors;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Tests.UnitTest.Application.Behaviors;

public sealed class TeamAuthorizationBehaviorTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<ITeamRepository> _teamRepository = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly TeamAuthorizationBehavior<TestTeamScopedRequest, string> _sut;

    public TeamAuthorizationBehaviorTests()
    {
        _sut = new(_teamRepository.Object, _currentUser.Object, _cacheService.Object);
        _currentUser.SetupGet(x => x.UserId).Returns(_userId);
        _cacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<TeamRole?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<IReadOnlyCollection<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string key, Func<CancellationToken, Task<TeamRole?>> factory, TimeSpan expiration,
                IReadOnlyCollection<string>? tags, CancellationToken ct) => factory(ct));
    }

    private void SetupRole(Guid teamId, TeamRole? role) =>
        _teamRepository.Setup(x => x.GetActiveRoleAsync(teamId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_Throws401AndDoesNotCallNext()
    {
        _currentUser.SetupGet(x => x.UserId).Returns((Guid?)null);
        var nextCalled = false;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(new TestTeamScopedRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
        _teamRepository.Verify(x => x.GetActiveRoleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoActiveRole_Throws403AndDoesNotCallNext()
    {
        var request = new TestTeamScopedRequest();
        SetupRole(request.TeamId, null);
        var nextCalled = false;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.Handle(request, _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenUserRoleIsNotRequired_Throws403AndDoesNotCallNext()
    {
        var request = new TestTeamScopedRequest();
        SetupRole(request.TeamId, TeamRole.Viewer);
        var nextCalled = false;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.Handle(request, _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenUserHasRequiredRole_CallsNextAndReturnsItsResult()
    {
        var request = new TestTeamScopedRequest();
        SetupRole(request.TeamId, TeamRole.Member);
        var nextCalled = false;

        var result = await _sut.Handle(request, _ =>
        {
            nextCalled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("allowed", result);
    }

    [Fact]
    public async Task Handle_WhenRoleIsCached_DoesNotQueryRepository()
    {
        var request = new TestTeamScopedRequest();
        _cacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<TeamRole?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<IReadOnlyCollection<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamRole?)TeamRole.Member);
        var nextCalled = false;

        var result = await _sut.Handle(request, _ =>
        {
            nextCalled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("allowed", result);
        _teamRepository.Verify(x => x.GetActiveRoleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UsesTeamAndUserScopedCacheKeyAndTags()
    {
        var request = new TestTeamScopedRequest();
        SetupRole(request.TeamId, TeamRole.Member);

        await _sut.Handle(request, _ => Task.FromResult("allowed"), CancellationToken.None);

        _cacheService.Verify(x => x.GetOrCreateAsync(
            $"teammanager:auth:team:{request.TeamId:N}:user:{_userId:N}",
            It.IsAny<Func<CancellationToken, Task<TeamRole?>>>(),
            It.IsAny<TimeSpan>(),
            It.Is<IReadOnlyCollection<string>?>(tags => tags != null
                && tags.Contains($"teammanager:auth:team:{request.TeamId:N}")
                && tags.Contains($"teammanager:auth:user:{_userId:N}")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed record TestTeamScopedRequest(Guid TeamId, TeamRole[] RequiredRoles)
        : IRequest<string>, ITeamScopedRequest
    {
        public TestTeamScopedRequest() : this(Guid.NewGuid(), [TeamRole.Member]) { }
    }
}
