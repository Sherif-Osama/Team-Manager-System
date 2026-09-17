using MediatR;
using TeamManager.Application.Abstractions.DefaultValues;
using TeamManager.Application.Common.Authorization;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Queries.GetUserByEmail
{
    public sealed record GetUserByEmailQuery(string Email) : IRequest<GetUserByEmailResponse>, IRequiresPermission
    {
        public string PermissionCode => PermissionCodes.ManageUsers;
    }
}