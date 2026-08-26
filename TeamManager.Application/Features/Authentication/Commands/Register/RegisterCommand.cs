using MediatR;

namespace TeamManager.Application.Features.Authentication.Commands.Register
{
    public sealed record RegisterCommand(string Email, string DisplayName, string Password) : IRequest<Guid>;
}