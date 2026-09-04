using MediatR;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Users.AdminUserManagement.Commands.AssignRole
{
    public sealed class AssignRoleCommandHandler(IUserRepository userRepository, IRoleRepository roleRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<AssignRoleCommand>
    {
        public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdWithRolesAsync(request.UserId, cancellationToken);

            if (user is null)
                throw new UserNotFoundException(request.UserId);

            var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

            if (role is null)
                throw new RoleNotFoundException(request.RoleId);

            user.AssignRole(role.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}