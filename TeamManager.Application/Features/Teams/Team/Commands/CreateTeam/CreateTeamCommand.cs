using MediatR;
using TeamManager.Application.Abstractions.Scopes;

namespace TeamManager.Application.Features.Teams.Team.Commands.CreateTeam
{
    public sealed record CreateTeamCommand(string Name, string? Description) : IRequest<Guid>, IRequiresConfirmedEmail;
}