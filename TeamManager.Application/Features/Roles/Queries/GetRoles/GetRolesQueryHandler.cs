using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Roles.Queries.GetRoles
{
    public sealed class GetRolesQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRolesQuery, GetRolesResponse>
    {
        public async Task<GetRolesResponse> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await context.Roles.AsNoTracking().OrderBy(r => r.Name)
                .Select(r => new RoleListItem(r.Id, r.Name, r.Description)).ToListAsync(cancellationToken);

            return new GetRolesResponse(roles);
        }
    }
}