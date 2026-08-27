namespace TeamManager.Application.Abstractions.Authentication
{
    public interface IRefreshTokenService
    {
        string GenerateToken();
        string HashToken(string token);
        DateTime GetExpiration();
    }
}