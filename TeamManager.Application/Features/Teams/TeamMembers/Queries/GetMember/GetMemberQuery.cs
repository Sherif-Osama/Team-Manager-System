using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMember
{
    public sealed record GetMemberQuery(Guid TeamId, long MemberId) : IRequest<GetMemberResponse>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => [TeamRole.Owner, TeamRole.Viewer, TeamRole.Admin];
    };
}