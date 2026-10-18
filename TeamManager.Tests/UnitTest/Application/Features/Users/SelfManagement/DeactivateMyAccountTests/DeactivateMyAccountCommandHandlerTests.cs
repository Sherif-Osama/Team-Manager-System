using Moq;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.UserExceptions;
using TeamManager.Application.Features.Users.SelfManagement.Commands.DeactivateMyAccount;
using TeamManager.Domain.Common.Events.Users;
using TeamManager.Domain.Entities;

namespace TeamManager.Tests.UnitTest.Application.Features.Users.SelfManagement.DeactivateMyAccountTests;

public sealed class DeactivateMyAccountCommandHandlerTests
{
    private static readonly DeactivateMyAccountCommand Request = new("Password123!");
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly DeactivateMyAccountCommandHandler _sut;

    public DeactivateMyAccountCommandHandlerTests()
    {
        _sut = new(_currentUser.Object, _userRepository.Object, _unitOfWork.Object, _passwordHasher.Object);
        _currentUser.SetupGet(x => x.UserId).Returns(_userId);
        _passwordHasher.Setup(x => x.Verify(Request.CurrentPassword, It.IsAny<string>())).Returns(true);
        _userRepository.Setup(x => x.IsLastSystemAdminAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _unitOfWork.Setup(x => x.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
    }

    private User CreateUser() => new(_userId, "user@example.com", "Test User", "password-hash", roleId: 1);

    private void SetupUser(User user) =>
        _userRepository.Setup(x => x.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_Throws401()
    {
        _currentUser.SetupGet(x => x.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.Handle(Request, CancellationToken.None));

        _userRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_Throws404()
    {
        _userRepository.Setup(x => x.GetByIdAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.Handle(Request, CancellationToken.None));

        _unitOfWork.Verify(x => x.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsWrong_Throws403WithoutStartingTransaction()
    {
        var user = CreateUser();
        SetupUser(user);
        _passwordHasher.Setup(x => x.Verify(Request.CurrentPassword, user.PasswordHash)).Returns(false);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.Handle(Request, CancellationToken.None));

        _unitOfWork.Verify(x => x.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIsLastSystemAdmin_Throws403AndDoesNotDeactivate()
    {
        var user = CreateUser();
        SetupUser(user);
        _userRepository.Setup(x => x.IsLastSystemAdminAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.Handle(Request, CancellationToken.None));

        Assert.True(user.IsActive);
        _userRepository.Verify(x => x.RevokeAllRefreshTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_DeactivatesRevokesTokensAndRaisesEvent()
    {
        var user = CreateUser();
        SetupUser(user);

        await _sut.Handle(Request, CancellationToken.None);

        Assert.False(user.IsActive);
        _userRepository.Verify(x => x.RevokeAllRefreshTokensAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(user.DomainEvents, e => e is UserDeactivatedDomainEvent { UserId: var userId } && userId == user.Id);
    }

    [Fact]
    public async Task Handle_WhenTokenRevocationFails_Propagates()
    {
        var user = CreateUser();
        SetupUser(user);
        _userRepository.Setup(x => x.RevokeAllRefreshTokensAsync(_userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(Request, CancellationToken.None));
    }
}
