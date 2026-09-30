using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Labels.Commands.UpdateLabel
{
    public sealed record UpdateLabelCommand(Guid TeamId, long LabelId, string Name, string? ColorHex = null) : IRequest,
        ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Admin];
    }
}