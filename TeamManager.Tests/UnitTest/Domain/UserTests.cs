using TeamManager.Domain.Common.Events.Users;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.UnitTest.Domain
{
    public sealed class UserTests
    {
        #region Helper methods
        private static User CreateUser(string email = "user@example.com", string displayName = "Test User", string passwordHash = "password-hash", int roleId = 1)
        {
            return new(Guid.NewGuid(), email, displayName, passwordHash, roleId);
        }

        private static User CreateConfirmedUser()
        {
            var user = CreateUser();
            user.RequestEmailConfirmation("confirmation-token", DateTime.UtcNow.AddHours(1));
            user.ConfirmEmail("confirmation-token");
            return user;
        }
        #endregion

        #region Constructor
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
        }

        [Fact]
        public void Constructor_CreatesInitialRoleMembership()
        {
            var user = CreateUser(roleId: 1);

            var role = Assert.Single(user.UserRoles);
            Assert.Equal(user.Id, role.UserId);
            Assert.Equal(1, role.RoleId);
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
            Assert.Throws<DomainException>(() => CreateUser(email: email!, displayName: displayName!, passwordHash: passwordHash!));
        }
        #endregion

        #region Assign Role
        [Fact]
        public void AssignRole_WithNewRole_AddsRole()
        {
            var user = CreateUser();

            user.AssignRole(2);

            Assert.Equal(2, user.UserRoles.Count);
            Assert.Contains(user.UserRoles, role => role.RoleId == 2);
        }

        [Fact]
        public void AssignRole_WhenRoleAlreadyAssigned_Throws()
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() => user.AssignRole(1));
        }

        [Fact]
        public void AssignRole_WhenUserIsDeleted_Throws()
        {
            var user = CreateUser();
            user.SoftDelete();

            Assert.Throws<DomainException>(() => user.AssignRole(1));
        }
        #endregion

        #region Remove Role
        [Fact]
        public void RemoveRole_WhenUserHasMultipleRoles_RemovesRequestedRole()
        {
            var user = CreateUser();
            user.AssignRole(2);

            user.RemoveRole(1);

            var remainingRole = Assert.Single(user.UserRoles);
            Assert.Equal(2, remainingRole.RoleId);
            Assert.DoesNotContain(user.UserRoles, role => role.RoleId == 1);
        }

        [Fact]
        public void RemoveRole_WhenRemovingLastRole_Throws()
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() => user.RemoveRole(1));
        }

        [Fact]
        public void RemoveRole_WhenUserDoesNotHaveRole_Throws()
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() => user.RemoveRole(999));
        }

        [Fact]
        public void RemoveRole_WhenUserIsDeleted_Throws()
        {
            var user = CreateUser();
            user.SoftDelete();

            Assert.Throws<DomainException>(() => user.RemoveRole(user.UserRoles.First().RoleId));
        }
        #endregion

        #region Deactivate
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
        #endregion

        #region Activate
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
        #endregion

        #region Soft Delete
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
        #endregion

        #region Display Name
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
        #endregion

        #region Change Email
        [Fact]
        public void ChangeEmail_WithValidData_StoresPendingEmailAndToken()
        {
            var user = CreateUser();
            var expiry = DateTime.UtcNow.AddHours(1);

            user.ChangeEmail("new@example.com", "token-hash", expiry);

            Assert.Equal("user@example.com", user.Email);
            Assert.Equal("new@example.com", user.PendingEmail);
            Assert.Equal("token-hash", user.EmailConfirmationTokenHash);
            Assert.Equal(expiry, user.EmailConfirmationTokenExpiresAtUtc);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ChangeEmail_WithBlankEmail_Throws(string? email)
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() => user.ChangeEmail(email!, "token-hash", DateTime.UtcNow.AddHours(1)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ChangeEmail_WithBlankToken_Throws(string? tokenHash)
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() => user.ChangeEmail("new@example.com", tokenHash!, DateTime.UtcNow.AddHours(1)));
        }

        [Fact]
        public void ChangeEmail_WithExpiredToken_Throws()
        {
            var user = CreateUser();

            Assert.Throws<DomainException>(() => user.ChangeEmail("new@example.com", "token-hash", DateTime.UtcNow.AddSeconds(-1)));
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
        #endregion

        #region Request Email Confirmation
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
            var user = CreateConfirmedUser();

            Assert.Throws<DomainException>(() =>
                user.RequestEmailConfirmation("new-token-hash", DateTime.UtcNow.AddHours(1)));
        }

        [Fact]
        public void RequestEmailConfirmation_WhenInactive_Throws()
        {
            var user = CreateUser();
            user.Deactivate();

            Assert.Throws<DomainException>(() => user.RequestEmailConfirmation("token-hash", DateTime.UtcNow.AddHours(1)));
        }

        [Fact]
        public void RequestEmailConfirmation_WhenDeleted_Throws()
        {
            var user = CreateUser();
            user.SoftDelete();

            Assert.Throws<DomainException>(() => user.RequestEmailConfirmation("token-hash", DateTime.UtcNow.AddHours(1)));
        }
        #endregion

        #region Confirm Email
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
            user.RequestEmailConfirmation("token-hash", DateTime.UtcNow.AddMilliseconds(20));
            Thread.Sleep(50);

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
        #endregion

        #region Request Password Reset
        [Fact]
        public void RequestPasswordReset_WhenEmailConfirmed_StoresTokenAndReturnsTrue()
        {
            var user = CreateConfirmedUser();
            var expiry = DateTime.UtcNow.AddMinutes(30);

            var result = user.RequestPasswordReset("reset-token", expiry);

            Assert.True(result);
            Assert.Equal("reset-token", user.PasswordResetTokenHash);
            Assert.Equal(expiry, user.PasswordResetTokenExpiresAtUtc);
        }

        [Fact]
        public void RequestPasswordReset_WhenEmailNotConfirmed_ReturnsFalse()
        {
            var user = CreateUser();

            Assert.False(user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddMinutes(30)));
            Assert.Null(user.PasswordResetTokenHash);
        }

        [Fact]
        public void RequestPasswordReset_WhenInactive_ReturnsFalse()
        {
            var user = CreateConfirmedUser();
            user.Deactivate();

            Assert.False(user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddMinutes(30)));
        }

        [Fact]
        public void RequestPasswordReset_WhenDeleted_ReturnsFalse()
        {
            var user = CreateConfirmedUser();
            user.SoftDelete();

            Assert.False(user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddMinutes(30)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RequestPasswordReset_WithBlankToken_ReturnsFalse(string? tokenHash)
        {
            var user = CreateConfirmedUser();

            Assert.False(user.RequestPasswordReset(tokenHash!, DateTime.UtcNow.AddMinutes(30)));
        }

        [Fact]
        public void RequestPasswordReset_WithExpiredDate_ReturnsFalse()
        {
            var user = CreateConfirmedUser();

            Assert.False(user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddSeconds(-1)));
        }
        #endregion

        #region Reset Password
        [Fact]
        public void ResetPassword_WithValidToken_ChangesHashAndClearsToken()
        {
            var user = CreateConfirmedUser();
            user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddMinutes(30));

            user.ResetPassword("reset-token", "new-password-hash");

            Assert.Equal("new-password-hash", user.PasswordHash);
            Assert.Null(user.PasswordResetTokenHash);
            Assert.Null(user.PasswordResetTokenExpiresAtUtc);
        }

        [Fact]
        public void ResetPassword_WithWrongToken_Throws()
        {
            var user = CreateConfirmedUser();
            user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddMinutes(30));

            Assert.Throws<DomainException>(() => user.ResetPassword("wrong-token", "new-password-hash"));
            Assert.Equal("password-hash", user.PasswordHash);
        }

        [Fact]
        public void ResetPassword_WithExpiredToken_Throws()
        {
            var user = CreateConfirmedUser();
            user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddMilliseconds(20));
            Thread.Sleep(50);

            Assert.Throws<DomainException>(() => user.ResetPassword("reset-token", "new-password-hash"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ResetPassword_WithBlankNewHash_Throws(string? newPasswordHash)
        {
            var user = CreateConfirmedUser();
            user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddMinutes(30));

            Assert.Throws<DomainException>(() => user.ResetPassword("reset-token", newPasswordHash!));
        }

        [Fact]
        public void ResetPassword_WithoutRequestedReset_Throws()
        {
            var user = CreateConfirmedUser();

            Assert.Throws<DomainException>(() => user.ResetPassword("reset-token", "new-password-hash"));
        }
        #endregion

        #region Has Valid Password Reset Token
        [Fact]
        public void HasValidPasswordResetToken_WithMatchingToken_ReturnsTrue()
        {
            var user = CreateConfirmedUser();
            user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddMinutes(30));

            Assert.True(user.HasValidPasswordResetToken("reset-token"));
        }

        [Fact]
        public void HasValidPasswordResetToken_WithWrongToken_ReturnsFalse()
        {
            var user = CreateConfirmedUser();
            user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddMinutes(30));

            Assert.False(user.HasValidPasswordResetToken("wrong-token"));
        }

        [Fact]
        public void HasValidPasswordResetToken_WhenExpired_ReturnsFalse()
        {
            var user = CreateConfirmedUser();
            user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddMilliseconds(20));
            Thread.Sleep(50);

            Assert.False(user.HasValidPasswordResetToken("reset-token"));
        }

        [Fact]
        public void HasValidPasswordResetToken_WhenNoneRequested_ReturnsFalse()
        {
            var user = CreateConfirmedUser();

            Assert.False(user.HasValidPasswordResetToken("reset-token"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void HasValidPasswordResetToken_WithBlankToken_ReturnsFalse(string? tokenHash)
        {
            var user = CreateConfirmedUser();
            user.RequestPasswordReset("reset-token", DateTime.UtcNow.AddMinutes(30));

            Assert.False(user.HasValidPasswordResetToken(tokenHash!));
        }
        #endregion

        #region Change Password
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
        #endregion

        #region Login Attempts
        [Fact]
        public void RecordSuccessfulLogin_ResetsFailuresLockoutAndRecordsTime()
        {
            var user = CreateUser();

            for (var i = 0; i < 5; i++)
                user.RecordFailedLoginAttempt();

            Assert.True(user.IsLockedOut);
            Assert.Equal(5, user.FailedLoginAttempts);

            user.RecordSuccessfulLogin();

            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LockoutEndUtc);
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
        #endregion

        #region Domain Events
        [Fact]
        public void Deactivate_RaisesUserDeactivatedDomainEvent()
        {
            var user = CreateUser();

            user.Deactivate();

            Assert.Contains(user.DomainEvents, e => e is UserDeactivatedDomainEvent);
        }

        [Fact]
        public void SoftDelete_RaisesUserDeletedDomainEvent()
        {
            var user = CreateUser();

            user.SoftDelete();

            Assert.Contains(user.DomainEvents, e => e is UserDeletedDomainEvent);
        }

        [Fact]
        public void RecordSuccessfulLogin_OnInactiveUser_RaisesUserReactivatedDomainEvent()
        {
            var user = CreateUser();
            user.Deactivate();

            user.RecordSuccessfulLogin();

            Assert.Contains(user.DomainEvents, e => e is UserReactivatedDomainEvent);
        }

        [Fact]
        public void AssignRole_RaisesUserRoleAssignedDomainEvent()
        {
            var user = CreateUser();

            user.AssignRole(2);

            Assert.Contains(user.DomainEvents, e => e is UserRoleAssignedDomainEvent);
        }

        [Fact]
        public void RemoveRole_RaisesUserRoleRevokedDomainEvent()
        {
            var user = CreateUser();
            user.AssignRole(2);

            user.RemoveRole(1);

            Assert.Contains(user.DomainEvents, e => e is UserRoleRevokedDomainEvent);
        }
        #endregion
    }
}
