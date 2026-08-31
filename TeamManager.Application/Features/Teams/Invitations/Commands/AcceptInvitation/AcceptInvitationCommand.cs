using MediatR;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.AcceptInvitation
{
    public sealed record AcceptInvitationCommand(string Token) : IRequest;
}