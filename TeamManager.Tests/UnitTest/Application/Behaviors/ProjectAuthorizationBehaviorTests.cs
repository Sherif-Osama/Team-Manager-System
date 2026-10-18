using MediatR;
using Moq;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.Authorization.AuthorizationInfo;
using TeamManager.Application.Common.Behaviors;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.ProjectExceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Tests.UnitTest.Application.Behaviors;

public sealed class ProjectAuthorizationBehaviorTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly ProjectAuthorizationBehavior<TestProjectScopedRequest, string> _sut;

    public ProjectAuthorizationBehaviorTests()
    {
        _sut = new(_currentUser.Object, _projectRepository.Object, _cacheService.Object);
        _currentUser.SetupGet(x => x.UserId).Returns(_userId);
        _currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        _cacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<ProjectMembershipInfo?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<IReadOnlyCollection<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string key, Func<CancellationToken, Task<ProjectMembershipInfo?>> factory, TimeSpan expiration,
                IReadOnlyCollection<string>? tags, CancellationToken ct) => factory(ct));
    }

    private void SetupMembership(ProjectMembershipInfo? info) =>
        _projectRepository.Setup(x => x.GetMembershipInfoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(info);

    private static ProjectMembershipInfo Info(ProjectRole? role, bool isTeamOwner) =>
        new(role, isTeamOwner, Guid.NewGuid());

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_Throws401AndDoesNotCallNext()
    {
        _currentUser.SetupGet(x => x.UserId).Returns((Guid?)null);
        var nextCalled = false;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(new TestProjectScopedRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
        _projectRepository.Verify(x => x.GetMembershipInfoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIdentityIsNotAuthenticated_Throws401()
    {
        _currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        var nextCalled = false;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(new TestProjectScopedRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenProjectDoesNotExist_ThrowsProjectNotFound()
    {
        SetupMembership(null);
        var nextCalled = false;

        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            _sut.Handle(new TestProjectScopedRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoRoleAndIsNotTeamOwner_Throws403()
    {
        SetupMembership(Info(null, false));
        var nextCalled = false;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.Handle(new TestProjectScopedRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenUserRoleIsNotRequired_Throws403()
    {
        SetupMembership(Info(ProjectRole.Viewer, false));
        var nextCalled = false;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.Handle(new TestProjectScopedRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenUserHasRequiredRole_CallsNextAndReturnsItsResult()
    {
        SetupMembership(Info(ProjectRole.Member, false));
        var nextCalled = false;

        var result = await _sut.Handle(new TestProjectScopedRequest(), _ =>
        {
            nextCalled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("allowed", result);
    }

    [Fact]
    public async Task Handle_WhenUserIsTeamOwnerWithoutProjectRole_CallsNext()
    {
        SetupMembership(Info(null, true));
        var nextCalled = false;

        var result = await _sut.Handle(new TestProjectScopedRequest(), _ =>
        {
            nextCalled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("allowed", result);
    }

    [Fact]
    public async Task Handle_WhenMembershipIsCached_DoesNotQueryRepository()
    {
        _cacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<ProjectMembershipInfo?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<IReadOnlyCollection<string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Info(ProjectRole.Member, false));
        var nextCalled = false;

        var result = await _sut.Handle(new TestProjectScopedRequest(), _ =>
        {
            nextCalled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("allowed", result);
        _projectRepository.Verify(x => x.GetMembershipInfoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UsesProjectAndUserScopedCacheKeyAndTags()
    {
        var request = new TestProjectScopedRequest();
        SetupMembership(Info(ProjectRole.Member, false));

        await _sut.Handle(request, _ => Task.FromResult("allowed"), CancellationToken.None);

        _cacheService.Verify(x => x.GetOrCreateAsync(
            $"teammanager:auth:project:{request.ProjectId:N}:user:{_userId:N}",
            It.IsAny<Func<CancellationToken, Task<ProjectMembershipInfo?>>>(),
            It.IsAny<TimeSpan>(),
            It.Is<IReadOnlyCollection<string>?>(tags => tags != null
                && tags.Contains($"teammanager:auth:project:{request.ProjectId:N}")
                && tags.Contains($"teammanager:auth:user:{_userId:N}")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed record TestProjectScopedRequest(Guid ProjectId, ProjectRole[] RequiredRoles)
        : IRequest<string>, IProjectScopedRequest
    {
        public TestProjectScopedRequest() : this(Guid.NewGuid(), [ProjectRole.Member]) { }
    }
}
