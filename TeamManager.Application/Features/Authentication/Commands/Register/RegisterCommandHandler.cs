using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Common.Outbox;
using TeamManager.Domain.Entities;

namespace TeamManager.Application.Features.Authentication.Commands.Register
{
    public sealed class RegisterCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork, ITeamRepository teamRepository, IEmailConfirmationTokenService tokenService,
        IOutbox outbox) : IRequestHandler<RegisterCommand, Guid>
    {
        public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var exists = await userRepository.ExistsByEmailAsync(request.Email, cancellationToken);

            if (exists) throw new EmailAlreadyExistsException(request.Email);

            var passwordHash = passwordHasher.Hash(request.Password);

            var user = new User(Guid.NewGuid(), request.Email, request.DisplayName, passwordHash);

            var token = tokenService.GenerateToken();

            var tokenHash = tokenService.HashToken(token);

            user.RequestEmailConfirmation(tokenHash, DateTime.UtcNow.AddHours(24));

            await userRepository.AddAsync(user, cancellationToken);
            await teamRepository.LinkPendingInvitationsToUserAsync(request.Email, user.Id, cancellationToken);

            var payload = JsonSerializer.Serialize(new { To = request.Email, Token = token });

            outbox.Add(OutboxMessageType.EmailConfirmationEmail, payload);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return user.Id;
        }
    }
}