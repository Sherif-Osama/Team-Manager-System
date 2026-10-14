using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Outbox;
using TeamManager.Domain.Exceptions;

namespace TeamManager.Application.Features.Authentication.Commands.ResetPassword
{
    public sealed class ResetPasswordCommandHandler(IUserRepository userRepository, IEmailConfirmationTokenService tokenService,
        IPasswordHasher passwordHasher, IUnitOfWork unitOfWork, IOutbox outbox) : IRequestHandler<ResetPasswordCommand>
    {
        private const string InvalidTokenMessage = "The password reset token is invalid or expired.";

        public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);

            if (user is null)
                throw new DomainException(InvalidTokenMessage);

            var tokenHash = tokenService.HashToken(request.Token);

            if (!user.HasValidPasswordResetToken(tokenHash))
                throw new DomainException(InvalidTokenMessage);

            var newPasswordHash = passwordHasher.Hash(request.NewPassword);

            var payload = JsonSerializer.Serialize(new
            {
                To = user.Email
            });

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                user.ResetPassword(tokenHash, newPasswordHash);

                await userRepository.RevokeAllRefreshTokensAsync(user.Id, ct);

                await outbox.Add(OutboxMessageType.PasswordChangedNotificationEmail, payload);
            }, cancellationToken);
        }
    }
}