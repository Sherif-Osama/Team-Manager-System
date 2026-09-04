using MediatR;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.ConfirmEmail
{
    public sealed record ConfirmEmailCommand(string Token) : IRequest;
}