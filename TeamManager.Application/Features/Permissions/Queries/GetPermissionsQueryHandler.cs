using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Permissions.Queries
{
    public sealed class GetPermissionsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPermissionsQuery, GetPermissionsResponse>
    {
        public async Task<GetPermissionsResponse> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
        {
            var permissions = await context.Permissions.AsNoTracking().OrderBy(p => p.Id)
                .Select(p => new PermissionItemResponse(p.Id, p.Code, p.Description)).ToListAsync(cancellationToken);

            return new GetPermissionsResponse(permissions);
        }
    }
}