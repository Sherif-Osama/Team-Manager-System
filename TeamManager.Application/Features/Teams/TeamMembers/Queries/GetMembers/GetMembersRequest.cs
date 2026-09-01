using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMembers
{
    public sealed record GetMembersRequest(TeamMemberStatus? Status = null, int Page = 1, int PageSize = 20);
}