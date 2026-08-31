using MediatR;

namespace TeamManager.Application.Features.Teams.Invitations.Commands.RejectInvitation
{
    public sealed record RejectInvitationCommand(string Token) : IRequest;

}
