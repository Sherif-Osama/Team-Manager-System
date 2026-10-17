using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Permissions.Queries.GetPermissionById
{
    public sealed record GetPermissionByIdQuery(int PermissionId) : IRequest<GetPermissionByIdResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageRoles;
    }
}
