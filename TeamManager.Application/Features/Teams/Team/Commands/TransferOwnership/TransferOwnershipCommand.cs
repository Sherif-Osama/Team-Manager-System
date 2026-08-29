using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Team.Commands.TransferOwnership
{
    public sealed record TransferOwnershipCommand(Guid TeamId, Guid NewOwnerUserId) : IRequest, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => new[] { TeamRole.Owner };
    };
}