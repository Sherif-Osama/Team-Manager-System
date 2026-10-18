using TeamManager.Domain.Common.Events.Roles;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.UnitTest.Domain
{
    /// <summary>
    /// Tests the Role aggregate: naming and permission management.
    /// RolePermission behaviors are tested through the Role aggregate.
    /// </summary>
    public sealed class RoleTests
    {
        #region Helper Methods
        private static Role CreateRole(string name = "CustomRole", string? description = null)
        {
            return new Role(name, description);
        }
        #endregion

        #region Constructor
        [Fact]
        public void Constructor_WithValidData_SetsNameAndDescription()
        {
            var role = CreateRole(description: "A custom role");

            Assert.Equal("CustomRole", role.Name);
            Assert.Equal("A custom role", role.Description);
            Assert.Empty(role.RolePermissions);
            Assert.Empty(role.UserRoles);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankName_Throws(string? name)
        {
            Assert.Throws<DomainException>(() => CreateRole(name: name!));
        }
        #endregion

        #region Rename
        [Fact]
        public void Rename_WithValidName_UpdatesName()
        {
            var role = CreateRole();

            role.Rename("RenamedRole");

            Assert.Equal("RenamedRole", role.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Rename_WithBlankName_Throws(string? name)
        {
            var role = CreateRole();

            Assert.Throws<DomainException>(() => role.Rename(name!));
        }
        #endregion

        #region Add Permission
        [Fact]
        public void AddPermission_WhenNotPresent_AddsPermissionAndRaisesEvent()
        {
            var role = CreateRole();

            role.AddPermission(3);

            var permission = Assert.Single(role.RolePermissions);
            Assert.Equal(role.Id, permission.RoleId);
            Assert.Equal(3, permission.PermissionId);
            Assert.Contains(role.DomainEvents, e => e is RolePermissionAddedDomainEvent
            {
                RoleId: var roleId, PermissionId: var permissionId
            } && roleId == role.Id && permissionId == 3);
        }

        [Fact]
        public void AddPermission_WithMultipleDistinctPermissions_KeepsAll()
        {
            var role = CreateRole();

            role.AddPermission(1);
            role.AddPermission(2);
            role.AddPermission(3);

            Assert.Equal(3, role.RolePermissions.Count);
        }

        [Fact]
        public void AddPermission_Duplicate_Throws()
        {
            var role = CreateRole();
            role.AddPermission(3);

            Assert.Throws<DomainException>(() => role.AddPermission(3));
            Assert.Single(role.RolePermissions);
        }
        #endregion

        #region Remove Permission
        [Fact]
        public void RemovePermission_WhenPresent_RemovesItAndRaisesEvent()
        {
            var role = CreateRole();
            role.AddPermission(3);

            role.RemovePermission(3);

            Assert.Empty(role.RolePermissions);
            Assert.Contains(role.DomainEvents, e => e is RolePermissionRemovedDomainEvent
            {
                RoleId: var roleId, PermissionId: var permissionId
            } && roleId == role.Id && permissionId == 3);
        }

        [Fact]
        public void RemovePermission_WhenNotPresent_Throws()
        {
            var role = CreateRole();

            Assert.Throws<DomainException>(() => role.RemovePermission(3));
        }
        #endregion
    }
}
