using MediatR;

namespace TeamManager.Application.Features.Users.Commands.DeactivateMyAccount
{
    public sealed record DeactivateMyAccountCommand(string CurrentPassword) : IRequest;
}