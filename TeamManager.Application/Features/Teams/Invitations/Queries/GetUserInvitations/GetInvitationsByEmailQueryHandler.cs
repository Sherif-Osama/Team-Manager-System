using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Teams.Invitations.Queries.GetUserInvitations
{
    public sealed class GetInvitationsByEmailQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetInvitationsByEmailQuery, GetInvitationsByEmailResponse>
    {
        public async Task<GetInvitationsByEmailResponse> Handle(GetInvitationsByEmailQuery request, CancellationToken cancellationToken)
        {

            var teamExists = await context.Teams.AnyAsync(x => x.Id == request.TeamId && x.DeletedAtUtc == null, cancellationToken);

            if (!teamExists)
                throw new TeamNotFoundException(request.TeamId);

            var invitations = context.TeamInvitations.AsNoTracking().Where(x => x.TeamId == request.TeamId && x.InvitedEmail == request.Email);

            if (request.Status.HasValue)
                invitations = invitations.Where(x => x.Status == request.Status.Value);


            var totalCount = await invitations.CountAsync(cancellationToken);

            var items = await invitations
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new InvitationItem(x.Id, x.TeamId, x.Team.Name, x.InvitedEmail, x.InvitedUserId, x.TeamRole,
                x.Status, x.ExpiresAtUtc, x.AcceptedAtUtc, x.RejectedAtUtc, x.CancelledAtUtc,
                x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetInvitationsByEmailResponse(items, request.Page, request.PageSize, totalCount);
        }
    }
}