using MediatR;
using TeamManager.Application.Abstractions.Caching;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.Caching;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Labels.Queries.GetLabel
{
    public sealed record GetLabelByIdQuery(Guid TeamId, long LabelId) : IRequest<GetLabelByIdResponse>, ITeamScopedRequest,
        ICacheableQuery
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Admin, TeamRole.Member, TeamRole.Viewer];

        public string CacheKey => $"teammanager:labels:team:{TeamId:N}:label:{LabelId}";

        public TimeSpan CacheExpiration => TimeSpan.FromMinutes(30);

        public IReadOnlyCollection<string>? Tags => [CacheTags.TeamLabels(TeamId)];
    }
}