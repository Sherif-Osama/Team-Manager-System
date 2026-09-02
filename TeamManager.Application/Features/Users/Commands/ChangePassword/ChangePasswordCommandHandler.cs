using MediatR;
using System.Text.Json;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Common.Outbox;

namespace TeamManager.Application.Features.Users.Commands.ChangePassword
{
    public sealed class ChangePasswordCommandHandler(ICurrentUser currentUser, IUserRepository userRepository,
        IPasswordHasher passwordHasher, IOutbox outbox, IUnitOfWork unitOfWork) : IRequestHandler<ChangePasswordCommand>
    {
        public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);

            if (user is null)
                throw new UserNotFoundException(currentUser.UserId.Value);

            var isPasswordValid = passwordHasher.Verify(request.CurrentPassword, user.PasswordHash);

            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Invalid password.");

            var newPasswordHash = passwordHasher.Hash(request.NewPassword);

            user.ChangePasswordHash(newPasswordHash);

            await userRepository.RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

            var payload = JsonSerializer.Serialize(new { To = user.Email });

            outbox.Add(OutboxMessageType.PasswordChangedNotificationEmail, payload);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}