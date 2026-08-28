using MediatR;

namespace TeamManager.Application.Features.Teams.Commands.ActivateTeam
{
    public sealed record ActivateTeamCommand(Guid TeamId) : IRequest;
}