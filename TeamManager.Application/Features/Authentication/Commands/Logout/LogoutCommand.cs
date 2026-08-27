using MediatR;

namespace TeamManager.Application.Features.Authentication.Commands.Logout
{
    public sealed record LogoutCommand(string RefreshToken) : IRequest;
}