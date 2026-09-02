using MediatR;

namespace TeamManager.Application.Features.Users.Commands.ChangeEmail
{
    public sealed record ChangeEmailCommand(string CurrentPassword, string NewEmail) : IRequest;
}