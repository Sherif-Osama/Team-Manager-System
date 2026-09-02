using MediatR;

namespace TeamManager.Application.Features.Users.Commands.UpdateMyProfile
{
    public sealed record UpdateMyProfileCommand(string DisplayName) : IRequest;
}