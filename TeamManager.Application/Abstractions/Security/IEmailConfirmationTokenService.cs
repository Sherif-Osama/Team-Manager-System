namespace TeamManager.Application.Abstractions.Security
{
    public interface IEmailConfirmationTokenService
    {
        string GenerateToken();
        string HashToken(string token);
    }
}