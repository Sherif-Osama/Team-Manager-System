using MediatR;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetMyInvitations
{
    public sealed record GetMyInvitationsQuery(TeamInvitationStatus? Status = null, int Page = 1, int PageSize = 20)
        : IRequest<GetMyInvitationsResponse>;
}