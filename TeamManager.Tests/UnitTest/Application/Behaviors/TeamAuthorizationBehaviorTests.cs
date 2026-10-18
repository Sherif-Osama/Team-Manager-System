using MediatR;
using Moq;
using TeamManager.Application.Abstractions.Authentication;
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
    private readonly TeamAuthorizationBehavior<TestTeamScopedRequest, string> _sut;

    public TeamAuthorizationBehaviorTests()
    {
        _sut = new(_teamRepository.Object, _currentUser.Object);
        _currentUser.SetupGet(x => x.UserId).Returns(_userId);
    }

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
        _teamRepository.Verify(x => x.HasActiveRoleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<IReadOnlyCollection<TeamRole>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoRequiredRole_Throws403AndDoesNotCallNext()
    {
        var request = new TestTeamScopedRequest();
        _teamRepository.Setup(x => x.HasActiveRoleAsync(request.TeamId, _userId,
            request.RequiredRoles, It.IsAny<CancellationToken>())).ReturnsAsync(false);
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
        _teamRepository.Setup(x => x.HasActiveRoleAsync(request.TeamId, _userId,
            request.RequiredRoles, It.IsAny<CancellationToken>())).ReturnsAsync(true);
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
    public async Task Handle_ChecksRequestTeamIdAndRequiredRoles()
    {
        var request = new TestTeamScopedRequest(Guid.NewGuid(), [TeamRole.Owner, TeamRole.Admin]);
        _teamRepository.Setup(x => x.HasActiveRoleAsync(request.TeamId, _userId,
            request.RequiredRoles, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _sut.Handle(request, _ => Task.FromResult("allowed"), CancellationToken.None);

        _teamRepository.Verify(x => x.HasActiveRoleAsync(request.TeamId, _userId,
            request.RequiredRoles, It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed record TestTeamScopedRequest(Guid TeamId, TeamRole[] RequiredRoles)
        : IRequest<string>, ITeamScopedRequest
    {
        public TestTeamScopedRequest() : this(Guid.NewGuid(), [TeamRole.Member]) { }
    }
}
