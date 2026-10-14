using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Outbox;

namespace TeamManager.Application.Features.Authentication.Commands.ForgotPassword
{
    public sealed class ForgotPasswordCommandHandler(IUserRepository userRepository, IEmailConfirmationTokenService tokenService,
        IOutbox outbox, IUnitOfWork unitOfWork) : IRequestHandler<ForgotPasswordCommand>
    {
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);

        public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim();

            var user = await userRepository.GetByEmailAsync(email, cancellationToken);

            if (user is null || !user.IsEmailConfirmed)
                return;

            var token = tokenService.GenerateToken();
            var tokenHash = tokenService.HashToken(token);
            var expiresAtUtc = DateTime.UtcNow.Add(TokenLifetime);

            bool tokenRequested = user.RequestPasswordReset(tokenHash, expiresAtUtc);

            if (!tokenRequested)
                return;

            var payload = JsonSerializer.Serialize(new
            {
                To = user.Email,
                Token = token
            });

            await outbox.Add(OutboxMessageType.PasswordResetEmail, payload);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}