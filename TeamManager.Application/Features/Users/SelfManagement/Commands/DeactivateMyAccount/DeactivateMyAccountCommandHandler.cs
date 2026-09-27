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

            var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);

            if (user is null)
                throw new UserNotFoundException(currentUser.UserId.Value);

            if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
                throw new ForbiddenException("Invalid password.");

            await unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var isLastSystemAdmin = await userRepository.IsLastSystemAdminAsync(user.Id, ct);

                if (isLastSystemAdmin)
                    throw new ForbiddenException("The last system administrator cannot deactivate their account.");

                user.Deactivate();

                await userRepository.RevokeAllRefreshTokensAsync(user.Id, ct);

            }, cancellationToken);
        }
    }
}