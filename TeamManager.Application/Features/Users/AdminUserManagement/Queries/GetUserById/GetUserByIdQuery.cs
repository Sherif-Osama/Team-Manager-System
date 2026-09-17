using MediatR;
using TeamManager.Application.Abstractions.DefaultValues;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(Guid UserId) : IRequest<GetUserByIdResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageUsers;
    }
}