using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Team.Commands.DeactivateTeam
{
    public sealed record DeactivateTeamCommand(Guid TeamId) : IRequest, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => new[] { TeamRole.Owner };
    };
}