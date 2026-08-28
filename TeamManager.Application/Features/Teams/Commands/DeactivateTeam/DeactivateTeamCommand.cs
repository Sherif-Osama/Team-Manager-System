using MediatR;

namespace TeamManager.Application.Features.Teams.Commands.DeactivateTeam
{
    public sealed record DeactivateTeamCommand(Guid TeamId) : IRequest;
}
