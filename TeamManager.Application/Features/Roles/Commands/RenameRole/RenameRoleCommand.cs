using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Roles.Commands.RenameRole
{
    public sealed record RenameRoleCommand(int RoleId, string NewName) : IRequest<int>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageRoles;
    }
}
