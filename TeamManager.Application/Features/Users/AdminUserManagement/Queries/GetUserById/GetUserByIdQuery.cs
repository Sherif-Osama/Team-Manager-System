using MediatR;
using TeamManager.Application.Common.Authorization.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(Guid UserId) : IRequest<GetUserByIdResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageUsers;
    }
}