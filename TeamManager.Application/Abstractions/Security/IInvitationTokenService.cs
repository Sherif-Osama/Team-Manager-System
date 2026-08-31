namespace TeamManager.Application.Abstractions.Security
{
    public interface IInvitationTokenService
    {
        string GenerateToken();
        string HashToken(string token);
    }
}