using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetInvitations
{
    public sealed record GetInvitationsResponse(IReadOnlyCollection<TeamInvitationItem> Items, int Page, int PageSize,
        int TotalCount);

    public sealed record TeamInvitationItem(Guid Id, string TeamName, string InvitedEmail, Guid? InvitedUserId, Guid InvitedBy, TeamRole TeamRole,
        TeamInvitationStatus Status, DateTime ExpiresAtUtc, DateTime? AcceptedAtUtc, DateTime? RejectedAtUtc,
        DateTime? CancelledAtUtc, DateTime CreatedAtUtc);
}