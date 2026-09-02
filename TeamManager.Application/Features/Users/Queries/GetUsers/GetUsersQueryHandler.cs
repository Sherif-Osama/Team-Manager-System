using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler(IApplicationDbContext context) : IRequestHandler<GetUsersQuery, GetUsersResponse>
{
    public async Task<GetUsersResponse> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = context.Users.AsNoTracking().Where(x => x.DeletedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x => x.DisplayName.Contains(search) || x.Email.Contains(search));
        }

        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);


        if (request.IsEmailConfirmed.HasValue)
            query = query.Where(x => x.IsEmailConfirmed == request.IsEmailConfirmed);

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query.OrderBy(x => x.DisplayName).ThenBy(x => x.Id).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new UserListItem(x.Id, x.Email, x.DisplayName, x.IsEmailConfirmed, x.IsActive,
            x.CreatedAtUtc, x.LastLoginUtc)).ToListAsync(cancellationToken);


        return new GetUsersResponse(users, request.Page, request.PageSize, totalCount);
    }
}