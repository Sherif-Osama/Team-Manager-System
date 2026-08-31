using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMembers
{
    public sealed record GetMembersResponse(IReadOnlyCollection<TeamMemberItem> Items, int Page, int PageSize, int TotalCount);

    public sealed record TeamMemberItem(long Id, Guid UserId, string DisplayName, string Email, TeamRole TeamRole,
        TeamMemberStatus Status, DateTime JoinedAtUtc);
}