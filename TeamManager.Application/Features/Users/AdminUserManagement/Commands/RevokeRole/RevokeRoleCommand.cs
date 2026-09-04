using MediatR;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Commands.RevokeRole
{
    public sealed record RevokeRoleCommand(Guid UserId, int RoleId) : IRequest, IRequiresPermission
    {
        public string PermissionCode => "system.manage_roles";
    }
}