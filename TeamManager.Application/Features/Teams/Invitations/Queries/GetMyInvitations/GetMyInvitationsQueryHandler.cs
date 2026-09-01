using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetMyInvitations
{
    public sealed class GetMyInvitationsQueryHandler(ICurrentUser currentUser, IApplicationDbContext context)
        : IRequestHandler<GetMyInvitationsQuery, GetMyInvitationsResponse>
    {
        public async Task<GetMyInvitationsResponse> Handle(GetMyInvitationsQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(currentUser.Email))
                throw new UnauthorizedAccessException("User email is not available.");

            var invitations = context.TeamInvitations.AsNoTracking().Where(x => x.InvitedEmail == currentUser.Email);

            if (request.Status.HasValue)
                invitations = invitations.Where(x => x.Status == request.Status.Value);

            var totalCount = await invitations.CountAsync(cancellationToken);

            var items = await invitations
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new MyInvitationItem(x.Id, x.TeamId, x.Team.Name, x.InvitedEmail, x.InvitedUserId, x.TeamRole,
                x.Status, x.ExpiresAtUtc, x.AcceptedAtUtc, x.RejectedAtUtc,
                x.CancelledAtUtc, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetMyInvitationsResponse(items, request.Page, request.PageSize, totalCount);
        }
    }
}