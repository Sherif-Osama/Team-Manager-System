using MediatR;
using Moq;
using TeamManager.Application.Abstractions.Authentication;
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
    private readonly ProjectAuthorizationBehavior<TestProjectScopedRequest, string> _sut;

    public ProjectAuthorizationBehaviorTests()
    {
        _sut = new(_currentUser.Object, _projectRepository.Object);
        _currentUser.SetupGet(x => x.UserId).Returns(_userId);
        _currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
    }

    private void SetupAuthorizationInfo(ProjectAuthorizationInfo? info) =>
        _projectRepository.Setup(x => x.GetAuthorizationInfoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<ProjectRole[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(info);

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
        _projectRepository.Verify(x => x.GetAuthorizationInfoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<ProjectRole[]>(), It.IsAny<CancellationToken>()), Times.Never);
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
        var request = new TestProjectScopedRequest();
        SetupAuthorizationInfo(null);
        var nextCalled = false;

        await Assert.ThrowsAsync<ProjectNotFoundException>(() =>
            _sut.Handle(request, _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoRoleAndIsNotTeamOwner_Throws403()
    {
        SetupAuthorizationInfo(new ProjectAuthorizationInfo(true, false, false));
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
        SetupAuthorizationInfo(new ProjectAuthorizationInfo(true, true, false));
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
        SetupAuthorizationInfo(new ProjectAuthorizationInfo(true, false, true));
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
    public async Task Handle_ChecksRequestProjectIdAndRequiredRoles()
    {
        var request = new TestProjectScopedRequest(Guid.NewGuid(), [ProjectRole.Owner, ProjectRole.Admin]);
        SetupAuthorizationInfo(new ProjectAuthorizationInfo(true, true, false));

        await _sut.Handle(request, _ => Task.FromResult("allowed"), CancellationToken.None);

        _projectRepository.Verify(x => x.GetAuthorizationInfoAsync(request.ProjectId, _userId,
            request.RequiredRoles, It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed record TestProjectScopedRequest(Guid ProjectId, ProjectRole[] RequiredRoles)
        : IRequest<string>, IProjectScopedRequest
    {
        public TestProjectScopedRequest() : this(Guid.NewGuid(), [ProjectRole.Member]) { }
    }
}
