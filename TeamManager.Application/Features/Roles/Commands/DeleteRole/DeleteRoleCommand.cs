using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Roles.Commands.DeleteRole
{
    public sealed record DeleteRoleCommand(int RoleId) : IRequest, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageRoles;
    }
}
