using Moq;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Common.Outbox;
using TeamManager.Application.Features.Authentication.Commands.Login;
using TeamManager.Domain.Entities;

namespace TeamManager.Tests.Authentication.LoginTests
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
        private readonly Mock<IOutbox> _outbox = new();
        private readonly Mock<ITeamRepository> _teamRepository = new();
        private readonly LoginCommandHandler _sut;

        public LoginCommandHandlerTests()
        {
            _sut = new(_userRepository.Object, _passwordHasher.Object, _accessTokenService.Object,
                _refreshTokenService.Object, _unitOfWork.Object, _currentUser.Object, _outbox.Object, _teamRepository.Object);
            //happy path
            _passwordHasher.Setup(x => x.Verify(Request.Password, It.IsAny<string>())).Returns(true);
            _accessTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
            _refreshTokenService.Setup(x => x.GenerateToken()).Returns("raw-refresh-token");
            _refreshTokenService.Setup(x => x.HashToken("raw-refresh-token")).Returns("hashed-refresh-token");
            _refreshTokenService.Setup(x => x.GetExpiration()).Returns(DateTime.UtcNow.AddDays(7));
            _currentUser.SetupGet(x => x.DeviceInfo).Returns("test-device");
            _currentUser.SetupGet(x => x.IpAddress).Returns("127.0.0.1");
            _unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
        }

        private User CreateUser() => new(Guid.NewGuid(), Request.Email, "Test User", "password-hash", roleId: 1);

        private void SetupTransactionTracking(Action onRollback)
        {
            _unitOfWork.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>(async (action, ct) =>
                {
                    try { await action(ct); }
                    catch (InvalidOperationException) { onRollback(); throw; }
                });
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ThrowsUnauthorized()
        {
            _userRepository.Setup(x => x.GetByEmailAsync(Request.Email, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.Handle(Request, CancellationToken.None));
            _passwordHasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenDeletedUserIsNotReturned_ThrowsUnauthorized()
        {
            _userRepository.Setup(x => x.GetByEmailAsync(Request.Email, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.Handle(Request, CancellationToken.None));
            _userRepository.Verify(x => x.GetByEmailAsync(Request.Email, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenUserIsLocked_ThrowsAccountLockedWithoutCheckingPassword()
        {
            var user = CreateUser();

            for (var i = 0; i < 5; i++)
                user.RecordFailedLoginAttempt();

            _userRepository.Setup(x => x.GetByEmailAsync(Request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

            await Assert.ThrowsAsync<AccountLockedException>(() => _sut.Handle(Request, CancellationToken.None));
            _passwordHasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _refreshTokenService.Verify(x => x.GenerateToken(), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenPasswordIsWrong_RecordsAttemptSavesAndDoesNotCreateRefreshToken()
        {
            var user = CreateUser();
            _userRepository.Setup(x => x.GetByEmailAsync(Request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _passwordHasher.Setup(x => x.Verify(Request.Password, user.PasswordHash)).Returns(false);
            _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
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
            _userRepository.Setup(x => x.GetByEmailAsync(Request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
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
            _teamRepository.Verify(x => x.ReactivateSuspendedMembershipsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _outbox.Verify(x => x.Add(It.IsAny<OutboxMessageType>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithActiveUser_DoesNotReactivateMembershipsOrSendActivationEmail()
        {
            var user = CreateUser();
            _userRepository.Setup(x => x.GetByEmailAsync(Request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            await _sut.Handle(Request, CancellationToken.None);
            Assert.True(user.IsActive);
            _teamRepository.Verify(x => x.ReactivateSuspendedMembershipsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _outbox.Verify(x => x.Add(OutboxMessageType.AccountActivationEmail, It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithInactiveUser_ReactivatesMembershipsAndQueuesActivationEmail()
        {
            var user = CreateUser();
            user.Deactivate();
            _userRepository.Setup(x => x.GetByEmailAsync(Request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            string? payload = null;
            _outbox.Setup(x => x.Add(OutboxMessageType.AccountActivationEmail, It.IsAny<string>()))
                .Callback<OutboxMessageType, string>((_, value) => payload = value);
            await _sut.Handle(Request, CancellationToken.None);
            Assert.True(user.IsActive);
            _teamRepository.Verify(x => x.ReactivateSuspendedMembershipsAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
            _outbox.Verify(x => x.Add(OutboxMessageType.AccountActivationEmail, It.IsAny<string>()), Times.Once);
            using var json = JsonDocument.Parse(payload!);
            Assert.Equal(user.Email, json.RootElement.GetProperty("To").GetString());
            Assert.Equal("test-device", json.RootElement.GetProperty("DeviceInfo").GetString());
        }

        [Fact]
        public async Task Handle_WhenRefreshTokenPersistenceFails_RollsBack()
        {
            var user = CreateUser();
            _userRepository.Setup(x => x.GetByEmailAsync(Request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _userRepository.Setup(x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
            var rolledBack = false;
            SetupTransactionTracking(() => rolledBack = true);
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(Request, CancellationToken.None));
            Assert.True(rolledBack);
            _teamRepository.Verify(x => x.ReactivateSuspendedMembershipsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _outbox.Verify(x => x.Add(It.IsAny<OutboxMessageType>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenMembershipReactivationFails_RollsBack()
        {
            var user = CreateUser();
            user.Deactivate();
            _userRepository.Setup(x => x.GetByEmailAsync(Request.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _teamRepository.Setup(x => x.ReactivateSuspendedMembershipsAsync(user.Id, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());
            var rolledBack = false;
            SetupTransactionTracking(() => rolledBack = true);
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(Request, CancellationToken.None));
            Assert.True(rolledBack);
            _userRepository.Verify(x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
            _outbox.Verify(x => x.Add(It.IsAny<OutboxMessageType>(), It.IsAny<string>()), Times.Never);
        }
    }
}
