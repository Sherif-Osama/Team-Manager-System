using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Common.Outbox;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.ChangeEmail
{
    public sealed class ChangeEmailCommandHandler(ICurrentUser currentUser, IUserRepository userRepository,
        IPasswordHasher passwordHasher, IEmailConfirmationTokenService tokenService, IOutbox outbox,
        IUnitOfWork unitOfWork) : IRequestHandler<ChangeEmailCommand>
    {
        public async Task Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);

            if (user is null)
                throw new UserNotFoundException(currentUser.UserId.Value);

            var isPasswordValid = passwordHasher.Verify(request.CurrentPassword, user.PasswordHash);

            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Invalid password.");

            var emailTaken = await userRepository.ExistsByEmailAsync(request.NewEmail, cancellationToken);

            if (emailTaken)
                throw new EmailAlreadyExistsException(request.NewEmail);

            var token = tokenService.GenerateToken();
            var tokenHash = tokenService.HashToken(token);

            user.ChangeEmail(request.NewEmail, tokenHash, DateTime.UtcNow.AddHours(1));

            var payload = JsonSerializer.Serialize(new
            {
                To = request.NewEmail,
                Token = token
            });

            outbox.Add(OutboxMessageType.EmailConfirmationEmail, payload);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}