using MediatR;

namespace TeamManager.Application.Features.Teams.Commands.CreateTeam
{
    public sealed record CreateTeamCommand(string Name, string? Description) : IRequest<Guid>;
}
