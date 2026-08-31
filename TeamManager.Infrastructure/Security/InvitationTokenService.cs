using System.Security.Cryptography;
using System.Text;
using TeamManager.Application.Abstractions.Security;

namespace TeamManager.Infrastructure.Security
{
    public sealed class InvitationTokenService : IInvitationTokenService
    {
        public string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(bytes);
        }

        public string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(bytes);
        }
    }
}