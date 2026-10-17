using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Roles.Queries.GetRoleById
{
    public sealed record GetRoleByIdQuery(int RoleId) : IRequest<GetRoleByIdResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageRoles;
    }
}
