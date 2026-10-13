using MediatR;
using TeamManager.Application.Abstractions.Scopes;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(Guid UserId) : IRequest<GetUserByIdResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageUsers;
    }
}