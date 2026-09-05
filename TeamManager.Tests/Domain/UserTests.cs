using TeamManager.Domain.Entities;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.Domain
{
    public sealed class UserTests
    {
        private User CreateUser() => new(Guid.NewGuid(), "user@example.com", "Test User", "password-hash");

        [Fact]
        public void Constructor_WithValidData_CreatesActiveUnconfirmedUser()
        {
            var user = CreateUser();

            Assert.NotEqual(Guid.Empty, user.Id);
            Assert.Equal("user@example.com", user.Email);
            Assert.Equal("Test User", user.DisplayName);
            Assert.Equal("password-hash", user.PasswordHash);
            Assert.True(user.IsActive);
            Assert.False(user.IsEmailConfirmed);
            Assert.Empty(user.UserRoles);
        }

        [Theory]
        [InlineData(null, "Test User", "password-hash")]
        [InlineData("", "Test User", "password-hash")]
        [InlineData("user@example.com", null, "password-hash")]
        [InlineData("user@example.com", "", "password-hash")]
        [InlineData("user@example.com", "Test User", null)]
        [InlineData("user@example.com", "Test User", "")]
        public void Constructor_WithRequiredValueMissing_Throws(string? email, string? displayName, string? passwordHash)
        {
            Assert.Throws<DomainException>(() =>
                new User(Guid.NewGuid(), email!, displayName!, passwordHash!));
        }

        [Fact]
        public void AssignRole_WithNewRole_AddsRole()
        {
            var user = CreateUser();
            user.AssignRole(1);

            var role = Assert.Single(user.UserRoles);
            Assert.Equal(user.Id, role.UserId);
            Assert.Equal(1, role.RoleId);
        }

        [Fact]
        public void AssignRole_WhenUserIsInactive_AddsRole()
        {
            var user = CreateUser();
            user.Deactivate();

            user.AssignRole(1);

            Assert.Contains(user.UserRoles, role => role.RoleId == 1);
        }

        [Fact]
        public void AssignRole_WhenRoleAlreadyAssigned_Throws()
        {
            var user = CreateUser();
            user.AssignRole(1);

            Assert.Throws<DomainException>(() => user.AssignRole(1));
        }

        [Fact]
        public void AssignRole_WhenUserIsDeleted_Throws()
        {
            var user = CreateUser();
            user.SoftDelete();

            Assert.Throws<DomainException>(() => user.AssignRole(1));
        }

        [Fact]
        public void RemoveRole_WhenUserHasMultipleRoles_RemovesRequestedRole()
        {
            var user = CreateUser();
            user.AssignRole(1);
            user.AssignRole(2);

            user.RemoveRole(1);

            var remainingRole = Assert.Single(user.UserRoles);
            Assert.Equal(2, remainingRole.RoleId);
            Assert.DoesNotContain(user.UserRoles, role => role.RoleId == 1);
        }

        [Fact]
        public void RemoveRole_WhenRoleIsNotAssigned_Throws()
        {
            var user = CreateUser();
            user.AssignRole(1);

            Assert.Throws<DomainException>(() => user.RemoveRole(2));
        }

        [Fact]
        public void RemoveRole_WhenRemovingLastRole_Throws()
        {
            var user = CreateUser();
            user.AssignRole(1);

            Assert.Throws<DomainException>(() => user.RemoveRole(1));
        }

        [Fact]
        public void RemoveRole_WhenUserIsDeleted_Throws()
        {
            var user = CreateUser();
            user.AssignRole(1);
            user.AssignRole(2);
            user.SoftDelete();

            Assert.Throws<DomainException>(() => user.RemoveRole(1));
        }

        [Fact]
        public void Deactivate_WhenActive_SetsInactive()
        {
            var user = CreateUser();
            user.Deactivate();

            Assert.False(user.IsActive);
        }

        [Fact]
        public void Deactivate_WhenAlreadyInactive_Throws()
        {
            var user = CreateUser();
            user.Deactivate();

            Assert.Throws<DomainException>(() => user.Deactivate());
        }

        [Fact]
        public void Activate_WhenInactive_SetsActive()
        {
            var user = CreateUser();
            user.Deactivate();

            user.Activate();

            Assert.True(user.IsActive);
        }

        [Fact]
        public void Activate_WhenAlreadyActive_Throws()
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() => user.Activate());
        }

        [Fact]
        public void Activate_WhenDeleted_Throws()
        {
            var user = CreateUser();
            user.SoftDelete();

            Assert.Throws<DomainException>(() => user.Activate());
        }

        [Fact]
        public void SoftDelete_WhenNotDeleted_SetsDeletedAndInactive()
        {
            var user = CreateUser();
            user.SoftDelete();

            Assert.NotNull(user.DeletedAtUtc);
            Assert.False(user.IsActive);
        }

        [Fact]
        public void SoftDelete_WhenAlreadyDeleted_Throws()
        {
            var user = CreateUser();
            user.SoftDelete();

            Assert.Throws<DomainException>(() => user.SoftDelete());
        }

        [Fact]
        public void ChangeDisplayName_WhenActive_ChangesNameAndTimestamp()
        {
            var user = CreateUser();
            user.ChangeDisplayName("Updated User");

            Assert.Equal("Updated User", user.DisplayName);
            Assert.NotNull(user.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ChangeDisplayName_WithBlankName_Throws(string? displayName)
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() => user.ChangeDisplayName(displayName!));
        }

        [Fact]
        public void ChangeDisplayName_WhenInactive_Throws()
        {
            var user = CreateUser();
            user.Deactivate();

            Assert.Throws<DomainException>(() => user.ChangeDisplayName("Updated User"));
        }

        [Fact]
        public void ChangeDisplayName_WhenDeleted_Throws()
        {
            var user = CreateUser();
            user.SoftDelete();

            Assert.Throws<DomainException>(() => user.ChangeDisplayName("Updated User"));
        }

        [Fact]
        public void ChangePasswordHash_WhenActive_ChangesHashAndTimestamp()
        {
            var user = CreateUser();
            user.ChangePasswordHash("new-password-hash");

            Assert.Equal("new-password-hash", user.PasswordHash);
            Assert.NotNull(user.UpdatedAtUtc);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ChangePasswordHash_WithBlankHash_Throws(string? hash)
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() => user.ChangePasswordHash(hash!));
        }

        [Fact]
        public void ChangePasswordHash_WhenInactive_Throws()
        {
            var user = CreateUser();
            user.Deactivate();

            Assert.Throws<DomainException>(() => user.ChangePasswordHash("new-hash"));
        }

        [Fact]
        public void ChangePasswordHash_WhenDeleted_Throws()
        {
            var user = CreateUser();
            user.SoftDelete();

            Assert.Throws<DomainException>(() => user.ChangePasswordHash("new-hash"));
        }

        [Fact]
        public void RequestEmailConfirmation_WithValidToken_StoresTokenAndExpiry()
        {
            var user = CreateUser();
            var expiry = DateTime.UtcNow.AddHours(1);

            user.RequestEmailConfirmation("token-hash", expiry);

            Assert.Equal("token-hash", user.EmailConfirmationTokenHash);
            Assert.Equal(expiry, user.EmailConfirmationTokenExpiresAtUtc);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RequestEmailConfirmation_WithBlankToken_Throws(string? tokenHash)
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() =>
                user.RequestEmailConfirmation(tokenHash!, DateTime.UtcNow.AddHours(1)));
        }

        [Fact]
        public void RequestEmailConfirmation_WithExpiredDate_Throws()
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() =>
                user.RequestEmailConfirmation("token-hash", DateTime.UtcNow.AddSeconds(-1)));
        }

        [Fact]
        public void RequestEmailConfirmation_WhenAlreadyConfirmed_Throws()
        {
            var user = CreateUser();
            user.RequestEmailConfirmation("token-hash", DateTime.UtcNow.AddHours(1));
            user.ConfirmEmail("token-hash");

            Assert.Throws<DomainException>(() =>
                user.RequestEmailConfirmation("new-token-hash", DateTime.UtcNow.AddHours(1)));
        }

        [Fact]
        public void ConfirmEmail_WithValidToken_ConfirmsAndClearsTokenData()
        {
            var user = CreateUser();
            user.RequestEmailConfirmation("token-hash", DateTime.UtcNow.AddHours(1));

            user.ConfirmEmail("token-hash");

            Assert.True(user.IsEmailConfirmed);
            Assert.Null(user.EmailConfirmationTokenHash);
            Assert.Null(user.EmailConfirmationTokenExpiresAtUtc);
        }

        [Fact]
        public void ConfirmEmail_WithWrongToken_Throws()
        {
            var user = CreateUser();
            user.RequestEmailConfirmation("token-hash", DateTime.UtcNow.AddHours(1));

            Assert.Throws<DomainException>(() => user.ConfirmEmail("wrong-token"));
        }

        [Fact]
        public void ConfirmEmail_WithExpiredToken_Throws()
        {
            var user = CreateUser();
            user.RequestEmailConfirmation("token-hash", DateTime.UtcNow.AddMilliseconds(10));
            Thread.Sleep(20);

            Assert.Throws<DomainException>(() => user.ConfirmEmail("token-hash"));
        }

        [Fact]
        public void ConfirmEmail_WithoutPendingToken_Throws()
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() => user.ConfirmEmail("token-hash"));
        }

        [Fact]
        public void ConfirmEmail_WhenDeleted_Throws()
        {
            var user = CreateUser();
            user.RequestEmailConfirmation("token-hash", DateTime.UtcNow.AddHours(1));
            user.SoftDelete();

            Assert.Throws<DomainException>(() => user.ConfirmEmail("token-hash"));
        }

        [Fact]
        public void ConfirmEmail_WithPendingEmail_ReplacesEmail()
        {
            var user = CreateUser();
            user.ChangeEmail("new@example.com", "token-hash", DateTime.UtcNow.AddHours(1));

            user.ConfirmEmail("token-hash");

            Assert.Equal("new@example.com", user.Email);
            Assert.Null(user.PendingEmail);
            Assert.True(user.IsEmailConfirmed);
        }

        [Fact]
        public void ChangeEmail_WithSameEmailCaseInsensitive_Throws()
        {
            var user = CreateUser();
            var originalUpdatedAt = user.UpdatedAtUtc;

            Assert.Throws<DomainException>(() => user.ChangeEmail("USER@EXAMPLE.COM", "token-hash", DateTime.UtcNow.AddHours(1)));
            Assert.Equal("user@example.com", user.Email);
            Assert.Null(user.PendingEmail);
            Assert.Null(user.EmailConfirmationTokenHash);
            Assert.Equal(originalUpdatedAt, user.UpdatedAtUtc);
        }

        [Fact]
        public void ChangeEmail_WhenInactive_Throws()
        {
            var user = CreateUser();
            user.Deactivate();

            Assert.Throws<DomainException>(() => user.ChangeEmail("new@example.com", "token-hash", DateTime.UtcNow.AddHours(1)));
        }

        [Fact]
        public void ChangeEmail_WhenDeleted_Throws()
        {
            var user = CreateUser();
            user.SoftDelete();

            Assert.Throws<DomainException>(() =>
                user.ChangeEmail("new@example.com", "token-hash", DateTime.UtcNow.AddHours(1)));
        }

        [Fact]
        public void RecordSuccessfulLogin_ResetsFailuresLockoutAndRecordsTime()
        {
            var user = CreateUser();

            //MaxFailedLoginAttempts is 5, so we need to record 5 failed attempts to lock the user out
            for (var i = 0; i < 5; i++)
                user.RecordFailedLoginAttempt();

            Assert.True(user.IsLockedOut);

            Assert.Equal(5, user.FailedLoginAttempts);

            user.RecordSuccessfulLogin();

            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LockoutEndUtc);
            Assert.NotNull(user.LastLoginUtc);
        }

        [Fact]
        public void RecordSuccessfulLogin_ReactivatesInactiveUser()
        {
            var user = CreateUser();
            user.Deactivate();

            user.RecordSuccessfulLogin();

            Assert.True(user.IsActive);
        }

        [Fact]
        public void RecordSuccessfulLogin_WhenDeleted_Throws()
        {
            var user = CreateUser();
            user.SoftDelete();

            Assert.Throws<DomainException>(() => user.RecordSuccessfulLogin());
        }

        [Fact]
        public void RecordFailedLoginAttempt_BeforeLimit_IncrementsWithoutLockout()
        {
            var user = CreateUser();

            for (var i = 0; i < 4; i++)
                user.RecordFailedLoginAttempt();

            Assert.Equal(4, user.FailedLoginAttempts);
            Assert.Null(user.LockoutEndUtc);
        }

        [Fact]
        public void RecordFailedLoginAttempt_AtLimit_CreatesLockout()
        {
            var user = CreateUser();

            for (var i = 0; i < 5; i++)
                user.RecordFailedLoginAttempt();

            Assert.Equal(5, user.FailedLoginAttempts);
            Assert.True(user.IsLockedOut);
            Assert.True(user.LockoutEndUtc > DateTime.UtcNow);
        }

        [Fact]
        public void IsLockedOut_WhenNoLockoutExists_ReturnsFalse()
        {
            Assert.False(CreateUser().IsLockedOut);
        }
    }
}