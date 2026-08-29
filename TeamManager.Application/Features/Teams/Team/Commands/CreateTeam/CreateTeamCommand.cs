using MediatR;

namespace TeamManager.Application.Features.Teams.Team.Commands.CreateTeam
{
    public sealed record CreateTeamCommand(string Name, string? Description) : IRequest<Guid>;
}
