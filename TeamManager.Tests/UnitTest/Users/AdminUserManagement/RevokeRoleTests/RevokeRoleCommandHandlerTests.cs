using MediatR;
using Moq;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.Behaviors;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Features.Users.AdminUserManagement.Commands.RevokeRole;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.UnitTest.Users.AdminUserManagement.RevokeRoleTests
{

    public sealed class RevokeRoleCommandHandlerTests
    {
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IRoleRepository> _roleRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly RevokeRoleCommandHandler _sut;

        public RevokeRoleCommandHandlerTests()
        {
            _sut = new(_userRepository.Object, _roleRepository.Object, _unitOfWork.Object);
            _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _unitOfWork.Setup(x => x.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
        }

        private User CreateUser() => new(_userId, "user@example.com", "Test User", "password-hash", roleId: 1);

        private User CreateUserWithRole(Role role)
        {
            var user = CreateUser();
            user.AssignRole(role.Id);
            return user;
        }

        private void Setup(User user, Role role)
        {
            _userRepository.Setup(x => x.GetByIdWithRolesAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _roleRepository.Setup(x => x.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        }

        [Fact]
        public async Task Handle_WhenActiveUser_RevokesRoleAndSavesOnce()
        {
            var role = new Role("Admin");
            var user = CreateUserWithRole(role);
            Setup(user, role);

            await _sut.Handle(new RevokeRoleCommand(_userId, role.Id), CancellationToken.None);

            Assert.DoesNotContain(user.UserRoles, x => x.RoleId == role.Id);
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenInactiveUser_RevokesRoleAndSavesOnce()
        {
            var role = new Role("Admin");
            var user = CreateUserWithRole(role);
            user.Deactivate();
            Setup(user, role);

            await _sut.Handle(new RevokeRoleCommand(_userId, role.Id), CancellationToken.None);

            Assert.DoesNotContain(user.UserRoles, x => x.RoleId == role.Id);
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenUserIsDeleted_Throws404()
        {
            _userRepository.Setup(x => x.GetByIdWithRolesAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.Handle(new RevokeRoleCommand(_userId, 2), CancellationToken.None));
            _roleRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenRoleDoesNotExist_Throws404()
        {
            var user = CreateUser();
            _userRepository.Setup(x => x.GetByIdWithRolesAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _roleRepository.Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

            await Assert.ThrowsAsync<RoleNotFoundException>(() => _sut.Handle(new RevokeRoleCommand(_userId, 99), CancellationToken.None));
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenRoleIsNotOwned_ThrowsDomainError()
        {
            var user = CreateUser();
            var role = new Role("Admin");
            Setup(user, role);

            await Assert.ThrowsAsync<DomainException>(() => _sut.Handle(new RevokeRoleCommand(_userId, role.Id), CancellationToken.None));
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenRevokingDefaultUserRole_Throws403()
        {
            var role = new Role("User");
            var user = CreateUserWithRole(role);
            Setup(user, role);

            await Assert.ThrowsAsync<ForbiddenException>(() => _sut.Handle(new RevokeRoleCommand(_userId, role.Id), CancellationToken.None));
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenRemovingLastRole_ThrowsDomainError()
        {
            var role = new Role("Admin");
            var user = new User(_userId, "user@example.com", "Test User", "password-hash", role.Id);
            Setup(user, role);

            await Assert.ThrowsAsync<DomainException>(() => _sut.Handle(new RevokeRoleCommand(_userId, role.Id), CancellationToken.None));
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSystemAdminHasAnotherAdmin_RevokesInsideSerializableTransaction()
        {
            var role = new Role("SystemAdmin");
            var user = CreateUserWithRole(role);
            Setup(user, role);
            _userRepository.Setup(x => x.IsLastSystemAdminAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            await _sut.Handle(new RevokeRoleCommand(_userId, role.Id), CancellationToken.None);

            Assert.DoesNotContain(user.UserRoles, x => x.RoleId == role.Id);
            _unitOfWork.Verify(x => x.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
            _userRepository.Verify(x => x.IsLastSystemAdminAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenSystemAdminIsLast_Throws403AndDoesNotRevoke()
        {
            var role = new Role("SystemAdmin");
            var user = CreateUserWithRole(role);
            Setup(user, role);
            _userRepository.Setup(x => x.IsLastSystemAdminAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            await Assert.ThrowsAsync<ForbiddenException>(() => _sut.Handle(new RevokeRoleCommand(_userId, role.Id), CancellationToken.None));

            Assert.Contains(user.UserRoles, x => x.RoleId == role.Id);
            _unitOfWork.Verify(x => x.ExecuteInSerializableTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenSaveFails_PropagatesFailureAndDoesNotCompleteRevoke()
        {
            var role = new Role("Admin");
            var user = CreateUserWithRole(role);
            Setup(user, role);
            _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(new RevokeRoleCommand(_userId, role.Id), CancellationToken.None));
            _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task PermissionBehavior_WhenPermissionMissing_RejectsWithoutCallingHandler()
        {
            var currentUser = new Mock<ICurrentUser>();

            currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

            _userRepository.Setup(x => x.HasPermissionAsync(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var behavior = new PermissionAuthorizationBehavior<RevokeRoleCommand, Unit>(currentUser.Object, _userRepository.Object);

            var nextCalled = false;

            await Assert.ThrowsAsync<ForbiddenException>(() => behavior.Handle(new RevokeRoleCommand(_userId, 2),
                _ =>
                {
                    nextCalled = true;
                    return Task.FromResult(Unit.Value);
                }, CancellationToken.None));

            Assert.False(nextCalled);

            _userRepository.Verify(x => x.HasPermissionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            //First Method called in RevokeRoleCommand
            _userRepository.Verify(x => x.GetByIdWithRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }


        [Fact]
        public void Command_RequiresRoleManagementPermission()
        {
            var command = new RevokeRoleCommand(_userId, 2);
            var permission = Assert.IsAssignableFrom<IRequiresPermission>(command);

            Assert.Equal("system.manage_roles", permission.PermissionCode);
        }
    }
}