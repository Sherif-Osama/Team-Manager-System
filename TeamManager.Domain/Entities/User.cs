using TeamManager.Domain.Common;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class User : Entity<Guid>
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<RefreshToken> _refreshTokens = new();
    private readonly List<UserRole> _userRoles = new();

    public string Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool IsEmailConfirmed { get; private set; }
    public bool IsActive { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEndUtc { get; private set; }
    public DateTime? LastLoginUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public string? PendingEmail { get; private set; }

    public string? EmailConfirmationTokenHash { get; private set; }
    public DateTime? EmailConfirmationTokenExpiresAtUtc { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public bool IsLockedOut => LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;

    private User() { }

    public User(Guid id, string email, string displayName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("A user must have an email address.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("A user must have a display name.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("A user must have a password hash.");

        Id = id;
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        IsEmailConfirmed = false;
        IsActive = true;
        FailedLoginAttempts = 0;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeDisplayName(string displayName)
    {
        if (DeletedAtUtc.HasValue)
            throw new DomainException("Cannot change display name for a deleted user.");

        if (!IsActive)
            throw new DomainException("Cannot change display name for an inactive user.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("A user must have a display name.");

        DisplayName = displayName;
        Touch();
    }

    public void ChangeEmail(string newEmail, string confirmationTokenHash, DateTime confirmationTokenExpiresAtUtc)
    {
        if (DeletedAtUtc.HasValue)
            throw new DomainException("Cannot change email for a deleted user.");
        if (!IsActive)
            throw new DomainException("Cannot change email for an inactive user.");
        if (string.IsNullOrWhiteSpace(newEmail))
            throw new DomainException("A user must have an email address.");
        if (string.IsNullOrWhiteSpace(confirmationTokenHash))
            throw new DomainException("A confirmation token hash is required.");
        if (confirmationTokenExpiresAtUtc <= DateTime.UtcNow)
            throw new DomainException("A confirmation token cannot be created already expired.");
        if (string.Equals(Email, newEmail, StringComparison.OrdinalIgnoreCase))
            return;

        PendingEmail = newEmail;
        EmailConfirmationTokenHash = confirmationTokenHash;
        EmailConfirmationTokenExpiresAtUtc = confirmationTokenExpiresAtUtc;
        Touch();
    }

    public void ConfirmEmail(string tokenHash)
    {
        if (DeletedAtUtc.HasValue)
            throw new DomainException("Cannot confirm email for a deleted user.");

        if (EmailConfirmationTokenHash is null || EmailConfirmationTokenExpiresAtUtc is null)
            throw new DomainException("There is no pending email confirmation.");

        if (EmailConfirmationTokenExpiresAtUtc <= DateTime.UtcNow)
            throw new DomainException("The email confirmation link has expired.");

        if (!string.Equals(EmailConfirmationTokenHash, tokenHash, StringComparison.Ordinal))
            throw new DomainException("Invalid confirmation token.");

        if (PendingEmail is not null)
        {
            Email = PendingEmail;
            PendingEmail = null;
        }

        IsEmailConfirmed = true;
        EmailConfirmationTokenHash = null;
        EmailConfirmationTokenExpiresAtUtc = null;
        Touch();
    }

    public void RequestEmailConfirmation(string confirmationTokenHash, DateTime confirmationTokenExpiresAtUtc)
    {
        if (DeletedAtUtc.HasValue)
            throw new DomainException("Cannot request email confirmation for a deleted user.");
        if (!IsActive)
            throw new DomainException("Cannot request email confirmation for an inactive user.");
        if (IsEmailConfirmed)
            throw new DomainException("Email is already confirmed.");
        if (string.IsNullOrWhiteSpace(confirmationTokenHash))
            throw new DomainException("A confirmation token hash is required.");
        if (confirmationTokenExpiresAtUtc <= DateTime.UtcNow)
            throw new DomainException("A confirmation token cannot be created already expired.");

        EmailConfirmationTokenHash = confirmationTokenHash;
        EmailConfirmationTokenExpiresAtUtc = confirmationTokenExpiresAtUtc;
        Touch();
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        if (DeletedAtUtc.HasValue)
            throw new DomainException("Cannot change password for a deleted user.");
        if (!IsActive)
            throw new DomainException("Cannot change password for an inactive user.");
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("A password hash cannot be empty.");

        PasswordHash = newPasswordHash;
        Touch();
    }

    public void RecordSuccessfulLogin()
    {
        if (DeletedAtUtc.HasValue)
            throw new DomainException("Cannot record login for a deleted user.");

        if (!IsActive)
        {
            IsActive = true;
            Touch();
        }

        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
        LastLoginUtc = DateTime.UtcNow;
    }

    public void RecordFailedLoginAttempt()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
        }
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("User is already deactivated.");

        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        if (DeletedAtUtc.HasValue)
            throw new DomainException("Cannot activate a deleted user.");

        if (IsActive)
            throw new DomainException("User is already active.");

        IsActive = true;
        Touch();
    }

    public void SoftDelete()
    {
        if (DeletedAtUtc.HasValue)
            throw new DomainException("The user is already deleted.");

        DeletedAtUtc = DateTime.UtcNow;
        IsActive = false;
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
