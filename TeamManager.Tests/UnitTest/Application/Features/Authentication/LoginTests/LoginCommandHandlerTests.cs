using Moq;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.UserExceptions;
using TeamManager.Application.Features.Authentication.Commands.Login;
using TeamManager.Domain.Common.Events.Users;
using TeamManager.Domain.Entities;

namespace TeamManager.Tests.UnitTest.Application.Features.Authentication.LoginTests
{
    public sealed class LoginCommandHandlerTests
    {
        private static readonly LoginCommand Request = new("user@example.com", "Password123!");
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly Mock<IAccessTokenService> _accessTokenService = new();
        private readonly Mock<IRefreshTokenService> _refreshTokenService = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<ICurrentUser> _currentUser = new();
        private readonly LoginCommandHandler _sut;

        public LoginCommandHandlerTests()
        {
            _sut = new(_userRepository.Object, _passwordHasher.Object, _accessTokenService.Object,
                _refreshTokenService.Object, _unitOfWork.Object, _currentUser.Object);
            //happy path
            _passwordHasher.Setup(x => x.Verify(Request.Password, It.IsAny<string>())).Returns(true);
            _accessTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
            _refreshTokenService.Setup(x => x.GenerateToken()).Returns("raw-refresh-token");
            _refreshTokenService.Setup(x => x.HashToken("raw-refresh-token")).Returns("hashed-refresh-token");
            _refreshTokenService.Setup(x => x.GetExpiration()).Returns(DateTime.UtcNow.AddDays(7));
            _currentUser.SetupGet(x => x.DeviceInfo).Returns("test-device");
            _currentUser.SetupGet(x => x.IpAddress).Returns("127.0.0.1");
        }

        private User CreateUser() => new(Guid.NewGuid(), Request.Email, "Test User", "password-hash", roleId: 1);

        private void SetupUser(User user) =>
            _userRepository.Setup(x => x.GetByEmailWithRolesAsync(Request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ThrowsUnauthorized()
        {
            _userRepository.Setup(x => x.GetByEmailWithRolesAsync(Request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.Handle(Request, CancellationToken.None));

            _passwordHasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenUserIsLocked_ThrowsAccountLockedWithoutCheckingPassword()
        {
            var user = CreateUser();

            for (var i = 0; i < 5; i++)
                user.RecordFailedLoginAttempt();

            SetupUser(user);

            await Assert.ThrowsAsync<AccountLockedException>(() => _sut.Handle(Request, CancellationToken.None));

            _passwordHasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _refreshTokenService.Verify(x => x.GenerateToken(), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenPasswordIsWrong_RecordsAttemptSavesAndDoesNotCreateTokens()
        {
            var user = CreateUser();
            SetupUser(user);
            _passwordHasher.Setup(x => x.Verify(Request.Password, user.PasswordHash)).Returns(false);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.Handle(Request, CancellationToken.None));

            Assert.Equal(1, user.FailedLoginAttempts);
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _refreshTokenService.Verify(x => x.GenerateToken(), Times.Never);
            _accessTokenService.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithValidPassword_ReturnsTokensAndStoresOnlyRefreshHash()
        {
            var user = CreateUser();
            SetupUser(user);
            RefreshToken? savedToken = null;
            _userRepository.Setup(x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
                .Callback<RefreshToken, CancellationToken>((token, _) => savedToken = token).Returns(Task.CompletedTask);

            var result = await _sut.Handle(Request, CancellationToken.None);

            Assert.Equal("access-token", result.AccessToken);
            Assert.Equal("raw-refresh-token", result.RefreshToken);
            Assert.NotNull(savedToken);
            Assert.Equal("hashed-refresh-token", savedToken!.TokenHash);
            Assert.NotEqual(result.RefreshToken, savedToken.TokenHash);
            Assert.Equal(user.Id, savedToken.UserId);
            Assert.Equal("test-device", savedToken.DeviceInfo);
            Assert.Equal("127.0.0.1", savedToken.IpAddress);
            Assert.NotNull(user.LastLoginUtc);
            Assert.Equal(0, user.FailedLoginAttempts);
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithActiveUser_DoesNotRaiseReactivatedEvent()
        {
            var user = CreateUser();
            SetupUser(user);

            await _sut.Handle(Request, CancellationToken.None);

            Assert.True(user.IsActive);
            Assert.DoesNotContain(user.DomainEvents, e => e is UserReactivatedDomainEvent);
        }

        [Fact]
        public async Task Handle_WithInactiveUser_ReactivatesAndRaisesReactivatedEvent()
        {
            var user = CreateUser();
            user.Deactivate();
            SetupUser(user);

            await _sut.Handle(Request, CancellationToken.None);

            Assert.True(user.IsActive);
            Assert.Contains(user.DomainEvents, e => e is UserReactivatedDomainEvent { UserId: var userId } && userId == user.Id);
        }

        [Fact]
        public async Task Handle_WhenRefreshTokenPersistenceFails_PropagatesAndDoesNotSave()
        {
            var user = CreateUser();
            SetupUser(user);
            _userRepository.Setup(x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(Request, CancellationToken.None));

            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
