using Moq;
using TeamManager.Application.Abstractions.Configuration;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Features.Admin.Commands.BootstrapAdmin;
using TeamManager.Domain.Entities;

namespace TeamManager.Tests.Admin.BootstrapAdminTests
{
    public sealed class BootstrapAdminCommandHandlerTests
    {
        private readonly Mock<IBootstrapSecretProvider> _secretProvider = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IRoleRepository> _roleRepository = new();
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly BootstrapAdminCommandHandler _sut;
        private readonly Role role;
        private readonly User user;

        private static readonly BootstrapAdminCommand Request = new("user@example.com", "secret");
        public BootstrapAdminCommandHandlerTests()
        {
            _sut = new(_secretProvider.Object, _unitOfWork.Object, _roleRepository.Object, _userRepository.Object);
            _secretProvider.SetupGet(s => s.AdminSecret).Returns("secret");
            role = new("SystemAdmin");
            _roleRepository.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(role);
            _roleRepository.Setup(r => r.ExistsAdminAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            user = new User(Guid.NewGuid(), Request.Email, "Test User", "hashed-password", 1);
            _userRepository.Setup(u => u.GetByEmailWithRolesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        }

        private void SetupRollbackTracking(Action onRollback)
        {
            _unitOfWork.Setup(u =>
                u.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>
                (
                    async (action, ct) =>
                    {
                        try
                        {
                            await action(ct);
                        }
                        catch (Exception)
                        { onRollback(); throw; }
                    }
                );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("wrong-secret")]
        public async Task Handle_WhenSecretIsMissingOrInvalid_Throws401(string? secret)
        {
            _secretProvider.SetupGet(s => s.AdminSecret).Returns(secret);
            await Assert.ThrowsAnyAsync<UnauthorizedAccessException>(() => _sut.Handle(Request, It.IsAny<CancellationToken>()));
            _roleRepository.Verify(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSystemAdminRoleIsMissing_ThrowsDefaultRoleNotFoundException()
        {
            _roleRepository.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Role)null!);

            await Assert.ThrowsAnyAsync<DefaultRoleNotFoundException>(() => _sut.Handle(Request,
                It.IsAny<CancellationToken>()));

            _roleRepository.Verify(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            _unitOfWork.Verify(x => x.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenAdminAlreadyExists_Throws403InsideSerializableTransaction()
        {
            _roleRepository.Setup(r => r.ExistsAdminAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var rolledBack = false;
            SetupRollbackTracking(() => { rolledBack = true; });

            await Assert.ThrowsAsync<ForbiddenException>(() => _sut.Handle(Request, CancellationToken.None));
            _unitOfWork.Verify(x => x.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(rolledBack);
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenUserDoesNotExist_Throws404()
        {
            _userRepository.Setup(u => u.GetByEmailWithRolesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null!);

            var rolledBack = false;
            SetupRollbackTracking(() => rolledBack = true);
            await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.Handle(Request, CancellationToken.None));
            Assert.True(rolledBack);
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithValidSecret_AssignsSystemAdminAndSavesInsideSerializableTransaction()
        {

            var rolledBack = false;

            SetupRollbackTracking(() => rolledBack = true);

            await _sut.Handle(Request, CancellationToken.None);

            Assert.Contains(user.UserRoles, x => x.RoleId == role.Id);
            _unitOfWork.Verify(x => x.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            Assert.False(rolledBack);
        }

        [Fact]
        public async Task Handle_WhenInactiveUser_ThrowsForbidden()
        {
            user.Deactivate();

            var rolledBack = false;

            SetupRollbackTracking(() => rolledBack = true);

            await Assert.ThrowsAnyAsync<ForbiddenException>(() => _sut.Handle(Request, CancellationToken.None));
            Assert.False(user.IsActive);
            Assert.True(rolledBack);
        }

        [Fact]
        public void Command_RequireConfirmedEmailOrJwtMarker()
        {
            Assert.True(typeof(IRequiresConfirmedEmail).IsAssignableFrom(typeof(BootstrapAdminCommand)));
        }
    }
}
