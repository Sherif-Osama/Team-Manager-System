using Moq;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.Behaviors;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Features.Users.AdminUserManagement.Commands.AssignRole;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.UnitTest.Users.AdminUserManagement.AssignRoleTests;

public sealed class AssignRoleCommandHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRoleRepository> _roleRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AssignRoleCommandHandler _sut;

    public AssignRoleCommandHandlerTests()
    {
        _sut = new(_userRepository.Object, _roleRepository.Object, _unitOfWork.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private User CreateUser(int? additionalRoleId = null)
    {
        var user = new User(_userId, "user@example.com", "Test User", "password-hash", roleId: 1);
        if (additionalRoleId.HasValue && additionalRoleId.Value != 1)
            user.AssignRole(additionalRoleId.Value);
        return user;
    }

    private void SetupUser(User user) =>
        _userRepository.Setup(x => x.GetByIdWithRolesAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

    private void Setup(User user, Role role)
    {
        SetupUser(user);
        _roleRepository.Setup(x => x.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
    }

    [Fact]
    public async Task Handle_WhenActiveUser_AssignsRoleAndSavesOnce()
    {
        var user = CreateUser();
        var role = new Role("Admin");
        Setup(user, role);

        await _sut.Handle(new AssignRoleCommand(_userId, role.Id), CancellationToken.None);

        Assert.Contains(user.UserRoles, x => x.RoleId == role.Id);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_Throws404()
    {
        _userRepository.Setup(x => x.GetByIdWithRolesAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(() => _sut.Handle(new AssignRoleCommand(_userId, 2), CancellationToken.None));
        _roleRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRoleDoesNotExist_Throws404()
    {
        var user = CreateUser();
        SetupUser(user);
        _roleRepository.Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Role?)null);

        await Assert.ThrowsAsync<RoleNotFoundException>(() => _sut.Handle(new AssignRoleCommand(_userId, 99), CancellationToken.None));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyHasRole_ThrowsDomainErrorAndDoesNotSave()
    {
        var role = new Role("Admin");
        var user = CreateUser(role.Id);
        Setup(user, role);

        await Assert.ThrowsAsync<DomainException>(() => _sut.Handle(new AssignRoleCommand(_userId, role.Id), CancellationToken.None));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Command_RequiresRoleManagementPermission()
    {
        var command = new AssignRoleCommand(_userId, 2);
        var permission = Assert.IsAssignableFrom<IRequiresPermission>(command);

        Assert.Equal("system.manage_roles", permission.PermissionCode);
    }

    [Fact]
    public async Task PermissionBehavior_WhenPermissionMissing_RejectsWithoutCallingHandler()
    {
        var currentUser = new Mock<ICurrentUser>();
        var caller = Guid.NewGuid();
        currentUser.SetupGet(x => x.UserId).Returns(caller);
        _userRepository.Setup(x => x.HasPermissionAsync(caller, "system.manage_roles", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var behavior = new PermissionAuthorizationBehavior<AssignRoleCommand, int>(currentUser.Object, _userRepository.Object);
        var nextCalled = false;

        await Assert.ThrowsAsync<ForbiddenException>(() => behavior.Handle(
            new AssignRoleCommand(_userId, 2),
            _ => { nextCalled = true; return Task.FromResult(1); },
            CancellationToken.None));

        Assert.False(nextCalled);
    }
}
