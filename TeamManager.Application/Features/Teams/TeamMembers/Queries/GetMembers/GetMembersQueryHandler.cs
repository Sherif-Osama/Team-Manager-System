using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Enums;

namespace TeamManager.Application.Features.Teams.TeamMembers.Queries.GetMembers
{
    public sealed class GetMembersQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetMembersQuery, GetMembersResponse>
    {
        public async Task<GetMembersResponse> Handle(GetMembersQuery request, CancellationToken cancellationToken)
        {
            var teamExists = await context.Teams.
                AnyAsync(x => x.Id == request.TeamId && x.DeletedAtUtc == null, cancellationToken);

            if (!teamExists)
                throw new TeamNotFoundException(request.TeamId);

            var membersQuery = context.TeamMembers.AsNoTracking().Where(x => x.TeamId == request.TeamId
            && x.Status == (request.MemberStatus ?? TeamMemberStatus.Active));

            var totalCount = await membersQuery.CountAsync(cancellationToken);

            var items = await membersQuery.OrderBy(x => x.JoinedAtUtc).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(x => new TeamMemberItem(x.Id, x.UserId, x.User.DisplayName,
                x.User.Email, x.TeamRole, x.Status, x.JoinedAtUtc)).ToListAsync(cancellationToken);

            return new GetMembersResponse(items, request.Page, request.PageSize, totalCount);
        }
    }
}