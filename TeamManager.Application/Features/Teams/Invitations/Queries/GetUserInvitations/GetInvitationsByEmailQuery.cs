using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetUserInvitations
{
    public sealed record GetInvitationsByEmailQuery(Guid TeamId, string Email, TeamInvitationStatus? Status = null,
        int Page = 1, int PageSize = 20) : IRequest<GetInvitationsByEmailResponse>, ITeamScopedRequest
    {
        public TeamRole[] RequiredRoles => new[] { TeamRole.Owner, TeamRole.Admin };
    };
}