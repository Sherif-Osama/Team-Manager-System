using MediatR;
using TeamManager.Application.Abstractions.Configuration;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Admin.Commands.BootstrapAdmin
{
    /// <summary>
    ///         // The name of the system role assigned by the one-time bootstrap process.
    //
    // This role must already exist in the database because the bootstrap operation
    // resolves it by name and uses its database-generated RoleId when assigning
    // the role to the selected user.
    //
    // The bootstrap endpoint is intentionally restricted to this specific
    // system role and is automatically disabled once a SystemAdmin assignment
    // already exists.
    //
    // If the SystemAdmin role is renamed or removed from the database, the
    // bootstrap process will fail until the corresponding seeded role is updated.
    // see TeamManager.Application.Features.Authentication.Commands.Register in the RegisterCommandHandler class
    // for a similar pattern.
    /// </summary>
    public sealed class BootstrapAdminCommandHandler(IBootstrapSecretProvider secretProvider,
        IUnitOfWork unitOfWork, IRoleRepository roleRepository, IUserRepository userRepository) : IRequestHandler<BootstrapAdminCommand>
    {
        private const string SystemAdminRoleName = "SystemAdmin";

        public async Task Handle(BootstrapAdminCommand request, CancellationToken cancellationToken)
        {
            var expectedSecret = secretProvider.AdminSecret;

            if (string.IsNullOrWhiteSpace(expectedSecret) || request.Secret != expectedSecret)
                throw new UnauthorizedAccessException("Invalid email or bootstrap secret.");

            var role = await roleRepository.GetByNameAsync(SystemAdminRoleName, cancellationToken);

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