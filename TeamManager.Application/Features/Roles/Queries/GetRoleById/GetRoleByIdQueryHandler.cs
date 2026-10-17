using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;

namespace TeamManager.Application.Features.Roles.Queries.GetRoleById
{
    public sealed class GetRoleByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRoleByIdQuery, GetRoleByIdResponse>
    {
        public async Task<GetRoleByIdResponse> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
        {
            var role = await context.Roles.AsNoTracking()
                .Where(r => r.Id == request.RoleId)
                .Select(r => new GetRoleByIdResponse(r.Id, r.Name, r.Description,
                    r.RolePermissions.OrderBy(rp => rp.PermissionId)
                        .Select(rp => new RolePermissionItemResponse(rp.PermissionId, rp.Permission.Code)).ToList()))
                .FirstOrDefaultAsync(cancellationToken);

            if (role is null)
                throw new RoleNotFoundException(request.RoleId);

            return role;
        }
    }
}
