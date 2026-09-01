using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMember
{
    public sealed class GetMemberQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetMemberQuery, GetMemberResponse>
    {
        public async Task<GetMemberResponse> Handle(GetMemberQuery request, CancellationToken cancellationToken)
        {
            var teamExists = await context.Teams.
                AnyAsync(x => x.Id == request.TeamId && x.DeletedAtUtc == null, cancellationToken);

            if (!teamExists)
                throw new TeamNotFoundException(request.TeamId);

            var member = await context.TeamMembers.AsNoTracking().Where(x => x.TeamId == request.TeamId && x.Id == request.MemberId
            && x.Status == TeamMemberStatus.Active)
           .Select(x => new GetMemberResponse(x.Id, x.UserId, x.User.DisplayName, x.User.Email, x.TeamRole,
           x.Status, x.JoinedAtUtc, x.InvitedBy, x.InvitedByUser != null ? x.InvitedByUser.DisplayName : null,
           x.RemovedAtUtc, x.RemovedBy, x.RemovedByUser != null ? x.RemovedByUser.DisplayName : null))
           .FirstOrDefaultAsync(cancellationToken);

            if (member is null)
                throw new TeamMemberNotFoundException(request.TeamId, request.MemberId);

            return member;
        }
    }
}