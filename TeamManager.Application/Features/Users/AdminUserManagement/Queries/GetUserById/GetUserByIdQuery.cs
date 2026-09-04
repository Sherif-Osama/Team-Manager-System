using MediatR;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(Guid UserId) : IRequest<GetUserByIdResponse>, IRequiresPermission
    {
        public string PermissionCode => "system.manage_users";
    }
}