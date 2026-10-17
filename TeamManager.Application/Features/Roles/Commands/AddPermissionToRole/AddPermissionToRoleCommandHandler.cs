using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.PermissionExceptions;

namespace TeamManager.Application.Features.Roles.Commands.AddPermissionToRole
{
    public sealed class AddPermissionToRoleCommandHandler(IRoleRepository roleRepository, IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<AddPermissionToRoleCommand>
    {
        public async Task Handle(AddPermissionToRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await roleRepository.GetByIdWithPermissionsAsync(request.RoleId, cancellationToken);

            if (role is null)
                throw new RoleNotFoundException(request.RoleId);

            if (role.Name == DefaultRoles.Admin || role.Name == DefaultRoles.User)
                throw new ForbiddenException("Built-in roles cannot be modified.");

            var permission = await permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);

            if (permission is null)
                throw new PermissionNotFoundException(request.PermissionId);

            role.AddPermission(request.PermissionId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
