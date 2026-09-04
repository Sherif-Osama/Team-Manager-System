using MediatR;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.ChangeEmail
{
    public sealed record ChangeEmailCommand(string CurrentPassword, string NewEmail) : IRequest;
}