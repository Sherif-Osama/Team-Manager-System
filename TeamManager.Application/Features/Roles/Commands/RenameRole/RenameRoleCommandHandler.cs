using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.RoleExceptions;

namespace TeamManager.Application.Features.Roles.Commands.RenameRole
{
    public sealed class RenameRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<RenameRoleCommand, int>
    {
        public async Task<int> Handle(RenameRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

            if (role is null)
                throw new RoleNotFoundException(request.RoleId);

            if (role.Name == DefaultRoles.Admin || role.Name == DefaultRoles.User)
                throw new ForbiddenException("Built-in roles cannot be renamed.");

            var newName = request.NewName.Trim();

            var existingRole = await roleRepository.GetByNameAsync(newName, cancellationToken);

            if (existingRole is not null && existingRole.Id != role.Id)
                throw new RoleNameAlreadyExistsException(newName);

            role.Rename(newName);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return role.Id;
        }
    }
}
