using MediatR;

namespace TeamManager.Application.Features.Teams.Commands.UpdateTeam
{
    public sealed record UpdateTeamCommand(Guid TeamId, string Name, string? Description) : IRequest;
}
