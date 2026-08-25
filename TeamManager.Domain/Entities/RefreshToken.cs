using TeamManager.Domain.Common;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Domain.Entities;

public class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public string? DeviceInfo { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public RefreshToken? ReplacedByToken { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;

    private RefreshToken()
    {
    }

    public RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAtUtc,
        string? deviceInfo = null, string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("A refresh token must have a token hash.");
        if (expiresAtUtc <= DateTime.UtcNow)
            throw new DomainException("A refresh token cannot be created already expired.");

        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Revoke(Guid? replacedByTokenId = null)
    {
        if (IsRevoked) return;

        RevokedAtUtc = DateTime.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}
