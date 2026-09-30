using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Labels.Commands.CreateLabel
{

    public sealed record CreateLabelCommand(Guid TeamId, string Name, string? ColorHex = null) : IRequest<long>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Admin];
    }
}