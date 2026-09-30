using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabel
{
    public sealed record GetLabelByIdQuery(Guid TeamId, long LabelId) : IRequest<GetLabelByIdResponse>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Admin, TeamRole.Member, TeamRole.Viewer];
    }
}