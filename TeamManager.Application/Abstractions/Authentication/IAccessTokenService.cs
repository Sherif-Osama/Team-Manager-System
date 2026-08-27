using TeamManager.Domain.Entities;

namespace TeamManager.Application.Abstractions.Authentication
{
    public interface IAccessTokenService
    {
        string GenerateAccessToken(User user);
    }
}