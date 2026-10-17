using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.RoleExceptions;

namespace TeamManager.Application.Features.Roles.Commands.DeleteRole
{
    public sealed class DeleteRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteRoleCommand>
    {
        public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

            if (role is null)
                throw new RoleNotFoundException(request.RoleId);

            if (role.Name == DefaultRoles.Admin || role.Name == DefaultRoles.User)
                throw new ForbiddenException("Built-in roles cannot be deleted.");

            var isAssignedToUsers = await roleRepository.IsAssignedToUsersAsync(role.Id, cancellationToken);

            if (isAssignedToUsers)
                throw new RoleInUseException(role.Id);

            roleRepository.Delete(role);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
