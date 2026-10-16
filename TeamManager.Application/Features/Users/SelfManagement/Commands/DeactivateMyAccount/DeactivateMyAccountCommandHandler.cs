using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions.AuthorizationExceptions;
using TeamManager.Application.Common.Exceptions.UserExceptions;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.DeactivateMyAccount
{
    public sealed class DeactivateMyAccountCommandHandler(ICurrentUser currentUser, IUserRepository
        userRepository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        : IRequestHandler<DeactivateMyAccountCommand>
    {
        public async Task Handle(DeactivateMyAccountCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);

            if (user is null)
                throw new UserNotFoundException(userId);

            if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
                throw new ForbiddenException("Invalid password.");

            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var transactionalUser = await userRepository.GetByIdAsync(userId, ct);

                if (transactionalUser is null)
                    throw new UserNotFoundException(userId);

                var isLastSystemAdmin = await userRepository.IsLastSystemAdminAsync(transactionalUser.Id, ct);

                if (isLastSystemAdmin)
                    throw new ForbiddenException("The last system administrator cannot deactivate their account.");

                transactionalUser.Deactivate();

                await userRepository.RevokeAllRefreshTokensAsync(transactionalUser.Id, ct);

            }, cancellationToken);
        }
    }
}
