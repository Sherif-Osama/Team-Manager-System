using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Team.Commands.TransferOwnership
{
    public sealed record TransferOwnershipCommand(Guid TeamId, Guid NewOwnerUserId) : IRequest, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner];
    };
}