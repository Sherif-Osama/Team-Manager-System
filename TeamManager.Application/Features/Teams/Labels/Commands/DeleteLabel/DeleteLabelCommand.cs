using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Labels.Commands.DeleteLabel
{
    public sealed record DeleteLabelCommand(Guid TeamId, long LabelId) : IRequest, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Admin];
    }
}