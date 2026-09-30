using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabels
{
    public sealed record GetLabelsQuery(Guid TeamId, string? Search, int Page = 1, int PageSize = 20)
        : IRequest<GetLabelsResponse>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Admin, TeamRole.Member, TeamRole.Viewer];
    }
}