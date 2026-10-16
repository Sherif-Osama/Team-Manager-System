using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabels
{
    public sealed record GetLabelsQuery(Guid TeamId, string? Search, int Page = 1, int PageSize = 20)
        : IRequest<GetLabelsResponse>, ITeamScopedRequest, ICacheableQuery
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Admin, TeamRole.Member, TeamRole.Viewer];

        public string CacheKey => $"teammanager:labels:team:{TeamId:N}:search:{Search?.Trim() ?? string.Empty}:" +
            $"page:{Page}:pageSize:{PageSize}";

        public TimeSpan CacheExpiration => TimeSpan.FromMinutes(30);

        public IReadOnlyCollection<string>? Tags => [CacheTags.TeamLabels(TeamId)];
    }
}