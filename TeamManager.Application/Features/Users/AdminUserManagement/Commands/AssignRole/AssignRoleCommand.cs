using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Commands.AssignRole
{
    public sealed record AssignRoleCommand(Guid UserId, int RoleId) : IRequest, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageRoles;
    }
}