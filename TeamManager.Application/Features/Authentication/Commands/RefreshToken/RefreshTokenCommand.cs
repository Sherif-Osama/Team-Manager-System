using MediatR;
using TeamManager.Application.Features.Authentication.Commands.Login;

namespace TeamManager.Application.Features.Authentication.Commands.RefreshToken
{
    public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResponse>;
}