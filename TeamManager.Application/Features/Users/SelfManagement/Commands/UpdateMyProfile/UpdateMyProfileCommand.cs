using MediatR;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.UpdateMyProfile
{
    public sealed record UpdateMyProfileCommand(string DisplayName) : IRequest;
}