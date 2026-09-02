using MediatR;

namespace TeamManager.Application.Features.Users.Commands.DeleteMyAccount
{
    public sealed record DeleteMyAccountCommand() : IRequest;
}