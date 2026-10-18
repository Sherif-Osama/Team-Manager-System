using TeamManager.Domain.Entities;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Tests.UnitTest.Domain
{
    /// <summary>
    /// Tests RefreshToken behavior directly because it is created by
    /// RefreshTokenService and revoked via bulk updates, outside the User aggregate.
    /// </summary>
    public sealed class RefreshTokenTests
    {
        #region Helper Methods
        private static RefreshToken CreateToken(string tokenHash = "token-hash",
            DateTime? expiresAtUtc = null, string? deviceInfo = null, string? ipAddress = null)
        {
            return new RefreshToken(Guid.NewGuid(), Guid.NewGuid(), tokenHash,
                expiresAtUtc ?? DateTime.UtcNow.AddDays(7), deviceInfo, ipAddress);
        }
        #endregion

        #region Constructor
        [Fact]
        public void Constructor_WithValidData_SetsFields()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddDays(7);
            var before = DateTime.UtcNow;

            var token = new RefreshToken(id, userId, "token-hash", expiresAt, "test-device", "127.0.0.1");

            Assert.Equal(id, token.Id);
            Assert.Equal(userId, token.UserId);
            Assert.Equal("token-hash", token.TokenHash);
            Assert.Equal(expiresAt, token.ExpiresAtUtc);
            Assert.Equal("test-device", token.DeviceInfo);
            Assert.Equal("127.0.0.1", token.IpAddress);
            Assert.InRange(token.CreatedAtUtc, before, DateTime.UtcNow);
            Assert.False(token.IsRevoked);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankHash_Throws(string? tokenHash)
        {
            Assert.Throws<DomainException>(() => CreateToken(tokenHash: tokenHash!));
        }

        [Fact]
        public void Constructor_WithExpiredAt_Throws()
        {
            Assert.Throws<DomainException>(() => CreateToken(expiresAtUtc: DateTime.UtcNow.AddSeconds(-1)));
        }
        #endregion

        #region State Computations
        [Fact]
        public void IsExpired_WhenNotExpired_ReturnsFalse()
        {
            var token = CreateToken();

            Assert.False(token.IsExpired);
        }

        [Fact]
        public void IsExpired_WhenExpired_ReturnsTrue()
        {
            var token = CreateToken(expiresAtUtc: DateTime.UtcNow.AddMilliseconds(200));
            Thread.Sleep(400);

            Assert.True(token.IsExpired);
        }

        [Fact]
        public void IsActive_WhenNotExpiredAndNotRevoked_ReturnsTrue()
        {
            var token = CreateToken();

            Assert.True(token.IsActive);
        }

        [Fact]
        public void IsActive_WhenRevoked_ReturnsFalse()
        {
            var token = CreateToken();
            token.Revoke();

            Assert.False(token.IsActive);
        }

        [Fact]
        public void IsActive_WhenExpired_ReturnsFalse()
        {
            var token = CreateToken(expiresAtUtc: DateTime.UtcNow.AddMilliseconds(200));
            Thread.Sleep(400);

            Assert.False(token.IsActive);
        }
        #endregion

        #region Revoke
        [Fact]
        public void Revoke_WhenActive_SetsRevokedAtAndReplacement()
        {
            var token = CreateToken();
            var replacementId = Guid.NewGuid();
            var before = DateTime.UtcNow;

            token.Revoke(replacementId);

            Assert.True(token.IsRevoked);
            Assert.NotNull(token.RevokedAtUtc);
            Assert.InRange(token.RevokedAtUtc!.Value, before, DateTime.UtcNow);
            Assert.Equal(replacementId, token.ReplacedByTokenId);
        }

        [Fact]
        public void Revoke_WithoutReplacement_LeavesReplacementNull()
        {
            var token = CreateToken();

            token.Revoke();

            Assert.Null(token.ReplacedByTokenId);
        }

        [Fact]
        public void Revoke_WhenAlreadyRevoked_IsIdempotent()
        {
            var token = CreateToken();
            var firstReplacement = Guid.NewGuid();
            token.Revoke(firstReplacement);
            var revokedAt = token.RevokedAtUtc;

            token.Revoke(Guid.NewGuid());

            Assert.Equal(revokedAt, token.RevokedAtUtc);
            Assert.Equal(firstReplacement, token.ReplacedByTokenId);
        }
        #endregion
    }
}
