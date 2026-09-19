using MediatR;
using TeamManager.Application.Abstractions.Configuration;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.DefaultValues;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Admin.Commands.BootstrapAdmin
{
    public sealed class BootstrapAdminCommandHandler(IBootstrapSecretProvider secretProvider,
        IUnitOfWork unitOfWork, IRoleRepository roleRepository, IUserRepository userRepository) : IRequestHandler<BootstrapAdminCommand>
    {
        public async Task Handle(BootstrapAdminCommand request, CancellationToken cancellationToken)
        {
            var expectedSecret = secretProvider.AdminSecret;

            if (string.IsNullOrWhiteSpace(expectedSecret) || request.Secret != expectedSecret)
                throw new UnauthorizedAccessException("Invalid email or bootstrap secret.");

            var role = await roleRepository.GetByNameAsync(DefaultRoles.Admin, cancellationToken);

            if (role is null)
                throw new DefaultRoleNotFoundException("role is not seeded.");

            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var alreadyBootstrapped = await roleRepository.ExistsAdminAsync(role.Id, ct);

                if (alreadyBootstrapped)
                    throw new ForbiddenException("An administrator already exists. Bootstrap is disabled.");

                var user = await userRepository.GetByEmailWithRolesAsync(request.Email, ct);

                if (user is null)
                    throw new UserNotFoundException(request.Email);

                if (!user.IsActive)
                    throw new ForbiddenException("User with email is inactive");

                user.AssignRole(role.Id);
            }, cancellationToken);
        }
    }
}