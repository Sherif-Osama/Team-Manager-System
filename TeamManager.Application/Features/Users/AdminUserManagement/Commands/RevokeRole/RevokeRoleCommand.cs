using MediatR;
using TeamManager.Application.Abstractions.DefaultValues;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Commands.RevokeRole
{
    public sealed record RevokeRoleCommand(Guid UserId, int RoleId) : IRequest, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageRoles;
    }
}