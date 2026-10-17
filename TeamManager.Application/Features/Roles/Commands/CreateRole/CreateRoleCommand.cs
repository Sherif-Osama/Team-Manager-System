using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Roles.Commands.CreateRole
{
    public sealed record CreateRoleCommand(string Name, string? Description = null) : IRequest<int>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageRoles;
    }
}