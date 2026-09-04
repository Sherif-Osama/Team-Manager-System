using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using TeamManager.Application.Abstractions.Authentication;

namespace TeamManager.Infrastructure.Services.AuthenticationServices
{

    public sealed class RefreshTokenService(IOptions<JwtOptions> options) : IRefreshTokenService
    {
        private readonly JwtOptions _options = options.Value;

        public string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        public string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(bytes);
        }

        public DateTime GetExpiration()
        {
            return DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
        }
    }
}