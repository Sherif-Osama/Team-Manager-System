namespace TeamManager.Application.Features.Authentication.Commands.Login
{
    public sealed record LoginResponse(string AccessToken, string RefreshToken);
}