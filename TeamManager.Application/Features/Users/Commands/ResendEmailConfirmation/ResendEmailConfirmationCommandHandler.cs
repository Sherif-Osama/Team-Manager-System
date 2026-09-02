using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Common.Outbox;

namespace TeamManager.Application.Features.Users.Commands.ResendEmailConfirmation
{
    public sealed class ResendEmailConfirmationCommandHandler(ICurrentUser currentUser,
        IUserRepository userRepository, IEmailConfirmationTokenService tokenService,
        IOutbox outbox, IUnitOfWork unitOfWork) : IRequestHandler<ResendEmailConfirmationCommand>
    {
        public async Task Handle(ResendEmailConfirmationCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);

            if (user is null)
                throw new UserNotFoundException(currentUser.UserId.Value);

            var token = tokenService.GenerateToken();

            var tokenHash = tokenService.HashToken(token);

            user.RequestEmailConfirmation(tokenHash, DateTime.UtcNow.AddHours(24));

            var targetEmail = user.PendingEmail ?? user.Email;

            var payload = JsonSerializer.Serialize(new { To = targetEmail, Token = token });

            outbox.Add(OutboxMessageType.EmailConfirmationEmail, payload);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}