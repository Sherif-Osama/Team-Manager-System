using MediatR;

namespace TeamManager.Application.Features.Teams.Commands.SoftDeleteTeam
{
    public sealed record SoftDeleteTeamCommand(Guid TeamId) : IRequest;
}
