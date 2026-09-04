using MediatR;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.DeactivateMyAccount
{
    public sealed record DeactivateMyAccountCommand(string CurrentPassword) : IRequest;
}