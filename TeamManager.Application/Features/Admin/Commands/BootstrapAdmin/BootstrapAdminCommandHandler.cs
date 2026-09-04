using MediatR;
using Microsoft.EntityFrameworkCore;
using TeamManager.Application.Abstractions.Configuration;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Application.Features.Admin.Commands.BootstrapAdmin
{
    public sealed class BootstrapAdminCommandHandler(IApplicationDbContext context, IBootstrapSecretProvider secretProvider,
        IUnitOfWork unitOfWork) : IRequestHandler<BootstrapAdminCommand>
    {
        // The name of the system role assigned by the one-time bootstrap process.
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
        private const string SystemAdminRoleName = "SystemAdmin";

        public async Task Handle(BootstrapAdminCommand request, CancellationToken cancellationToken)
        {
            var expectedSecret = secretProvider.AdminSecret;

            if (string.IsNullOrWhiteSpace(expectedSecret) || request.Secret != expectedSecret)
                throw new UnauthorizedAccessException("Invalid bootstrap secret.");

            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == SystemAdminRoleName, ct);

                if (role is null)
                    throw new DomainException("The SystemAdmin role is not seeded.");

                var alreadyBootstrapped = await context.UserRoles.AnyAsync(ur => ur.RoleId == role.Id, ct);

                if (alreadyBootstrapped)
                    throw new ForbiddenException("An administrator already exists. Bootstrap is disabled.");

                var user = await context.Users.Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.DeletedAtUtc == null, ct);

                if (user is null)
                    throw new UserNotFoundException(request.Email);

                user.AssignRole(role.Id);
            }, cancellationToken);
        }
    }
}
