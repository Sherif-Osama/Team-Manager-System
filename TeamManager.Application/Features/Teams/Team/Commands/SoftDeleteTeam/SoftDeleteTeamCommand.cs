using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Team.Commands.SoftDeleteTeam
{
    public sealed record SoftDeleteTeamCommand(Guid TeamId) : IRequest, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => new[] { TeamRole.Owner };
    };
}