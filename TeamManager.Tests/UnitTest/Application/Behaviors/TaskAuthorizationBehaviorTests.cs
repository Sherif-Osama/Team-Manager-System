using MediatR;
using Moq;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.Authorization.AuthorizationInfo;
using TeamManager.Application.Common.Behaviors;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.TaskExceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Tests.UnitTest.Application.Behaviors;

public sealed class TaskAuthorizationBehaviorTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<ITaskRepository> _taskRepository = new();
    private readonly TaskAuthorizationBehavior<TestTaskScopedRequest, string> _sut;

    public TaskAuthorizationBehaviorTests()
    {
        _sut = new(_taskRepository.Object, _currentUser.Object);
        _currentUser.SetupGet(x => x.UserId).Returns(_userId);
        _currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
    }

    private void SetupAuthorizationInfo(TaskAuthorizationInfo? info) =>
        _taskRepository.Setup(x => x.GetAuthorizationInfoAsync(It.IsAny<long>(), It.IsAny<Guid>(),
            It.IsAny<ProjectRole[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(info);

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_Throws401AndDoesNotCallNext()
    {
        _currentUser.SetupGet(x => x.UserId).Returns((Guid?)null);
        var nextCalled = false;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(new TestTaskScopedRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
        _taskRepository.Verify(x => x.GetAuthorizationInfoAsync(It.IsAny<long>(), It.IsAny<Guid>(),
            It.IsAny<ProjectRole[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenIdentityIsNotAuthenticated_Throws401()
    {
        _currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        var nextCalled = false;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(new TestTaskScopedRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenTaskDoesNotExist_ThrowsTaskNotFound()
    {
        SetupAuthorizationInfo(null);
        var nextCalled = false;

        await Assert.ThrowsAsync<TaskNotFoundException>(() =>
            _sut.Handle(new TestTaskScopedRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoRoleAndIsNotTeamOwner_Throws403()
    {
        SetupAuthorizationInfo(new TaskAuthorizationInfo(true, false, false));
        var nextCalled = false;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.Handle(new TestTaskScopedRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenUserHasRequiredRole_CallsNextAndReturnsItsResult()
    {
        SetupAuthorizationInfo(new TaskAuthorizationInfo(true, true, false));
        var nextCalled = false;

        var result = await _sut.Handle(new TestTaskScopedRequest(), _ =>
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
        SetupAuthorizationInfo(new TaskAuthorizationInfo(true, false, true));
        var nextCalled = false;

        var result = await _sut.Handle(new TestTaskScopedRequest(), _ =>
        {
            nextCalled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("allowed", result);
    }

    [Fact]
    public async Task Handle_ChecksRequestTaskIdAndRequiredRoles()
    {
        var request = new TestTaskScopedRequest(42, [ProjectRole.Owner, ProjectRole.Admin]);
        SetupAuthorizationInfo(new TaskAuthorizationInfo(true, true, false));

        await _sut.Handle(request, _ => Task.FromResult("allowed"), CancellationToken.None);

        _taskRepository.Verify(x => x.GetAuthorizationInfoAsync(request.TaskId, _userId,
            request.RequiredProjectRoles, It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed record TestTaskScopedRequest(long TaskId, ProjectRole[] RequiredProjectRoles)
        : IRequest<string>, ITaskScopedRequest
    {
        public TestTaskScopedRequest() : this(1, [ProjectRole.Member]) { }
    }
}
