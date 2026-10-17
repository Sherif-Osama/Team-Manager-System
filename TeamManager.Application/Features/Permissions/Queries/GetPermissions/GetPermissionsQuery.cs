using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Permissions.Queries.GetPermissions
{
    public sealed record GetPermissionsQuery : IRequest<GetPermissionsResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageRoles;
    }
}