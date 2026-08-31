using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetUserInvitations
{
    public sealed record GetInvitationsByEmailResponse(IReadOnlyCollection<InvitationItem> Items, int Page,
        int PageSize, int TotalCount);

    public sealed record InvitationItem(Guid Id, Guid TeamId, string TeamName, string InvitedEmail, Guid? InvitedUserId,
        TeamRole TeamRole, TeamInvitationStatus Status, DateTime ExpiresAtUtc, DateTime? AcceptedAtUtc,
        DateTime? RejectedAtUtc, DateTime? CancelledAtUtc, DateTime CreatedAtUtc);
}