using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetInvitations
{
    public sealed record GetInvitationsQuery(Guid TeamId, string? Search, TeamInvitationStatus? Status,
        TeamRole? Role, int Page = 1, int PageSize = 20) : IRequest<GetInvitationsResponse>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => new[] { TeamRole.Owner, TeamRole.Admin };
    }
}