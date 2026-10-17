using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.PermissionExceptions;

namespace TeamManager.Application.Features.Permissions.Queries.GetPermissionById
{
    public sealed class GetPermissionByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPermissionByIdQuery, GetPermissionByIdResponse>
    {
        public async Task<GetPermissionByIdResponse> Handle(GetPermissionByIdQuery request, CancellationToken cancellationToken)
        {
            var permission = await context.Permissions.AsNoTracking()
                .Where(p => p.Id == request.PermissionId)
                .Select(p => new GetPermissionByIdResponse(p.Id, p.Code, p.Description))
                .FirstOrDefaultAsync(cancellationToken);

            if (permission is null)
                throw new PermissionNotFoundException(request.PermissionId);

            return permission;
        }
    }
}
