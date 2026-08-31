using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetUserInvitations
{
    public sealed record GetInvitationsByEmailRequest(Guid TeamId, string Email, TeamInvitationStatus? Status,
        int Page = 1, int PageSize = 20);
}