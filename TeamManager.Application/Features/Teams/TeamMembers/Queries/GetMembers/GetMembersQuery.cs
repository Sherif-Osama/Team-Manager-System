using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMembers
{
    public sealed record GetMembersQuery(Guid TeamId, TeamMemberStatus? MemberStatus, int Page = 1, int PageSize = 20) : IRequest<GetMembersResponse>,
        ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => new[] { TeamRole.Owner, TeamRole.Viewer, TeamRole.Admin };
    };
}