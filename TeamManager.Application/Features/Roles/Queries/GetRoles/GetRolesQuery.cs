using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Roles.Queries.GetRoles
{
    public sealed record GetRolesQuery : IRequest<GetRolesResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageRoles;
    }
}