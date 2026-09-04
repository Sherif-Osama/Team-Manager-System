using MediatR;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.DeleteMyAccount
{
    public sealed record DeleteMyAccountCommand(string CurrentPassword) : IRequest;
}