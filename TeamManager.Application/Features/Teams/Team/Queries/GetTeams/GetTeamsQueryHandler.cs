using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Teams.Team.Queries.GetTeams
{
    public sealed class GetTeamsQueryHandler(IApplicationDbContext context)
        : IRequestHandler<GetTeamsQuery, GetTeamsResponse>
    {
        public async Task<GetTeamsResponse> Handle(GetTeamsQuery request, CancellationToken cancellationToken)
        {
            var query = context.Teams.AsNoTracking().Where(x => x.DeletedAtUtc == null);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(x => x.Name.Contains(request.Search.Trim()));


            if (request.IsActive.HasValue)
                query = query.Where(x => x.IsActive == request.IsActive.Value);


            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query.OrderByDescending(x => x.CreatedAtUtc).Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize).Select(x => new TeamListItem(x.Id, x.Name, x.Description, x.OwnerUserId,
                x.Owner.DisplayName, x.CreatedBy, x.Creator.DisplayName, x.CreatedAtUtc)).ToListAsync(cancellationToken);

            return new GetTeamsResponse(items, request.Page, request.PageSize, totalCount);
        }
    }
}