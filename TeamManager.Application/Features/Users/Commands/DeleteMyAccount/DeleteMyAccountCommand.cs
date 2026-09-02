using MediatR;

namespace TeamManager.Application.Features.Users.Commands.DeleteMyAccount
{
    public sealed record DeleteMyAccountCommand(string CurrentPassword) : IRequest;
}