using MediatR;
using TeamManager.Application.Common.Authorization;
using TeamManager.Application.Common.DefaultValues;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserByEmail
{
    public sealed record GetUserByEmailQuery(string Email) : IRequest<GetUserByEmailResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageUsers;
    }
}