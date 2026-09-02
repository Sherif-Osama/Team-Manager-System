using MediatR;

namespace TeamManager.Application.Features.Users.Commands.ConfirmEmail
{
    public sealed record ConfirmEmailCommand(string Token) : IRequest;
}