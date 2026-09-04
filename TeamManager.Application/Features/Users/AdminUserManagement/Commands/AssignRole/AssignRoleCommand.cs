using MediatR;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Commands.AssignRole
{
    public sealed record AssignRoleCommand(Guid UserId, int RoleId) : IRequest, IRequiresPermission
    {
        public string PermissionCode => "system.manage_roles";
    }
}