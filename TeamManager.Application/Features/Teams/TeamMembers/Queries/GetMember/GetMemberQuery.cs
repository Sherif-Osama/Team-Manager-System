using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMember
{
    public sealed record GetMemberQuery(Guid TeamId, long MemberId) : IRequest<GetMemberResponse>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => new[] { TeamRole.Owner, TeamRole.Viewer, TeamRole.Admin };
    };
}