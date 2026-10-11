using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.ActivityLog.Queries.GetTeamActivity
{
    public sealed record GetTeamActivityQuery(Guid TeamId, int Page = 1, int PageSize = 20)
        : IRequest<GetTeamActivityResponse>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Admin, TeamRole.Member];
    }
}