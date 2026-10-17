using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;

namespace TeamManager.Application.Features.Roles.Commands.RemovePermissionFromRole
{
    public sealed class RemovePermissionFromRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<RemovePermissionFromRoleCommand>
    {
        public async Task Handle(RemovePermissionFromRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await roleRepository.GetByIdWithPermissionsAsync(request.RoleId, cancellationToken);

            if (role is null)
                throw new RoleNotFoundException(request.RoleId);

            if (role.Name == DefaultRoles.Admin || role.Name == DefaultRoles.User)
                throw new ForbiddenException("Built-in roles cannot be modified.");

            role.RemovePermission(request.PermissionId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
