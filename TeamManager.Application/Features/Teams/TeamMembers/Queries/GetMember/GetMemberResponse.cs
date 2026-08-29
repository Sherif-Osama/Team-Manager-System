using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMember
{
    public sealed record GetMemberResponse(long Id, Guid UserId, string DisplayName, string Email,
        TeamRole TeamRole, TeamMemberStatus Status, DateTime JoinedAtUtc, Guid? InvitedBy, string? InvitedByName,
        DateTime? RemovedAtUtc, Guid? RemovedBy, string? RemovedByName);
}