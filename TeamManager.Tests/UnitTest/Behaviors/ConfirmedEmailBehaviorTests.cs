using MediatR;
using Moq;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.Behaviors;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Features.Admin.Commands.BootstrapAdmin;
using TeamManager.Domain.Entities;

namespace TeamManager.Tests.UnitTest.Behaviors;

public sealed class ConfirmedEmailBehaviorTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly ConfirmedEmailBehavior<TestConfirmedEmailRequest, string> _sut;

    public ConfirmedEmailBehaviorTests()
    {
        _sut = new(_currentUser.Object, _userRepository.Object);
        _currentUser.SetupGet(x => x.UserId).Returns(_userId);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_Throws401AndDoesNotCallNext()
    {
        _currentUser.SetupGet(x => x.UserId).Returns((Guid?)null);
        var nextCalled = false;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(new TestConfirmedEmailRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
        _userRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserDoesNotExist_Throws401AndDoesNotCallNext()
    {
        _userRepository.Setup(x => x.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var nextCalled = false;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(new TestConfirmedEmailRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenEmailIsNotConfirmed_Throws403AndDoesNotCallNext()
    {
        var user = CreateUser();
        SetupUser(user);
        var nextCalled = false;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.Handle(new TestConfirmedEmailRequest(), _ =>
            {
                nextCalled = true;
                return Task.FromResult("allowed");
            }, CancellationToken.None));

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenEmailIsConfirmed_CallsNextAndReturnsItsResult()
    {
        var user = CreateConfirmedUser();
        SetupUser(user);
        var nextCalled = false;

        var result = await _sut.Handle(new TestConfirmedEmailRequest(), _ =>
        {
            nextCalled = true;
            return Task.FromResult("allowed");
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("allowed", result);
    }

    [Fact]
    public void BootstrapAdminCommand_CurrentlyRequiresConfirmedEmail()
    {
        var command = new BootstrapAdminCommand("user@example.com", "secret");

        Assert.IsAssignableFrom<IRequiresConfirmedEmail>(command);
    }

    private User CreateUser() => new(_userId, "user@example.com", "Test User", "password-hash", roleId: 1);

    private User CreateConfirmedUser()
    {
        var user = CreateUser();
        user.RequestEmailConfirmation("confirmation-token-hash", DateTime.UtcNow.AddHours(1));
        user.ConfirmEmail("confirmation-token-hash");
        return user;
    }

    private void SetupUser(User user) =>
        _userRepository.Setup(x => x.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

    private sealed record TestConfirmedEmailRequest : IRequest<string>, IRequiresConfirmedEmail;
}
