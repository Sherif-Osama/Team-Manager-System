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

    public void ConfirmEmail()
    {
        IsEmailConfirmed = true;
        Touch();
    }

    public void ChangeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("A user must have a display name.");

        DisplayName = displayName;
        Touch();
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("A password hash cannot be empty.");

        PasswordHash = newPasswordHash;
        Touch();
    }

    public void RecordSuccessfulLogin()
    {
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
        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void SoftDelete()
    {
        DeletedAtUtc = DateTime.UtcNow;
        IsActive = false;
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
