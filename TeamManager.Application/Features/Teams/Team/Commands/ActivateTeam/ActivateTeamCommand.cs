using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Team.Commands.ActivateTeam
{
    public sealed record ActivateTeamCommand(Guid TeamId) : IRequest, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner];
    };
}