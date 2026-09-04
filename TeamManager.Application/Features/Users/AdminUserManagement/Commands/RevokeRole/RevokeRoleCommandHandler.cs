using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Commands.RevokeRole
{
    public sealed class RevokeRoleCommandHandler(IUserRepository userRepository, IRoleRepository roleRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<RevokeRoleCommand>
    {
        private const string SystemAdminRoleName = "SystemAdmin";
        private const string DefaultRoleName = "User";

        public async Task Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdWithRolesAsync(request.UserId, cancellationToken);

            if (user is null)
                throw new UserNotFoundException(request.UserId);

            var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

            if (role is null)
                throw new RoleNotFoundException(request.RoleId);

            if (role.Name == DefaultRoleName)
                throw new ForbiddenException("Cannot delete user default role.");

            if (role.Name == SystemAdminRoleName)
            {
                await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
                {
                    var isLastSystemAdmin = await userRepository.IsLastSystemAdminAsync(user.Id, ct);

                    if (isLastSystemAdmin)
                        throw new ForbiddenException("The last system administrator role cannot be revoked.");

                    user.RemoveRole(role.Id);
                }, cancellationToken);

                return;
            }

            user.RemoveRole(role.Id);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
