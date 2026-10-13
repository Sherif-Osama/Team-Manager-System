using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Team.Commands.UpdateTeam
{
    public sealed record UpdateTeamCommand(Guid TeamId, string Name, string? Description) : IRequest, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner];
    };
}