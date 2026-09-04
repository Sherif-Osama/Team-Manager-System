using MediatR;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserByEmail
{
    public sealed record GetUserByEmailQuery(string Email) : IRequest<GetUserByEmailResponse>, IRequiresPermission
    {
        public string PermissionCode => "system.manage_users";
    }
}