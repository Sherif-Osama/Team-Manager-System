using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetInvitations
{
    public sealed record GetInvitationsRequest(Guid TeamId, string? Search, TeamInvitationStatus? Status,
        TeamRole? Role, int Page = 1, int PageSize = 20);
}