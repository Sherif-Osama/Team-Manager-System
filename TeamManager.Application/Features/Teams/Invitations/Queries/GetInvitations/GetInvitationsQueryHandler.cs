using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetInvitations
{

    public sealed class GetInvitationsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetInvitationsQuery, GetInvitationsResponse>
    {
        public async Task<GetInvitationsResponse> Handle(GetInvitationsQuery request, CancellationToken cancellationToken)
        {
            var teamExists = await
                context.Teams.AnyAsync(x => x.Id == request.TeamId && x.DeletedAtUtc == null, cancellationToken);

            if (!teamExists)
                throw new TeamNotFoundException(request.TeamId);

            var invitationsQuery = context.TeamInvitations.AsNoTracking().Where(x => x.TeamId == request.TeamId);

            if (!string.IsNullOrWhiteSpace(request.Search))
                invitationsQuery = invitationsQuery.Where(x => x.InvitedEmail.Contains(request.Search.Trim()));

            if (request.Status.HasValue)
                invitationsQuery = invitationsQuery.Where(x => x.Status == request.Status.Value);

            if (request.Role.HasValue)
                invitationsQuery = invitationsQuery.Where(x => x.TeamRole == request.Role.Value);


            var totalCount = await invitationsQuery.CountAsync(cancellationToken);

            var items = await invitationsQuery
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new TeamInvitationItem(x.Id, x.Team.Name, x.InvitedEmail, x.InvitedUserId,
                x.InvitedBy, x.TeamRole, x.Status, x.ExpiresAtUtc, x.AcceptedAtUtc, x.RejectedAtUtc,
                x.CancelledAtUtc, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetInvitationsResponse(items, request.Page, request.PageSize, totalCount);

        }
    }
}