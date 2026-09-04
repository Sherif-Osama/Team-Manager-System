using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Common.Outbox;
using TeamManager.Domain.Entities;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Application.Features.Authentication.Commands.Register
{
    public sealed class RegisterCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork, ITeamRepository teamRepository, IEmailConfirmationTokenService tokenService,
        IRoleRepository roleRepository, IOutbox outbox) : IRequestHandler<RegisterCommand, Guid>
    {
        // The default role assigned to every newly registered user.
        // This role is resolved from the database by name and its RoleId is assigned
        // to the user through the UserRole relationship.
        //
        // The default registration role is intentionally fixed in the application
        // and is not configurable through the admin role-management features.
        // Administrators can create and assign additional roles to users, but they
        // cannot change the role automatically assigned during registration.
        //
        // If this role is renamed or removed from the database, user registration
        // will fail until the corresponding value and seeded role are updated.
        private const string DefaultRoleName = "User";

        public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var exists = await userRepository.ExistsByEmailAsync(request.Email, cancellationToken);

            if (exists) throw new EmailAlreadyExistsException(request.Email);

            var passwordHash = passwordHasher.Hash(request.Password);

            var role = await roleRepository.GetByNameAsync(DefaultRoleName, cancellationToken);

            if (role is null)
                throw new DomainException("The default User role is not seeded.");

            var user = new User(Guid.NewGuid(), request.Email, request.DisplayName, passwordHash);

            user.AssignRole(role.Id);

            var token = tokenService.GenerateToken();

            var tokenHash = tokenService.HashToken(token);

            user.RequestEmailConfirmation(tokenHash, DateTime.UtcNow.AddHours(24));

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await userRepository.AddAsync(user, ct);

                await unitOfWork.SaveChangesAsync(ct);

                await teamRepository.LinkPendingInvitationsToUserAsync(request.Email, user.Id, ct);

                var payload = JsonSerializer.Serialize(new { To = request.Email, Token = token });

                outbox.Add(OutboxMessageType.EmailConfirmationEmail, payload);
            }, cancellationToken);

            return user.Id;
        }
    }
}
