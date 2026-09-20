using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Commands.RevokeRole
{
    public sealed record RevokeRoleCommand(Guid UserId, int RoleId) : IRequest, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageRoles;
    }
}