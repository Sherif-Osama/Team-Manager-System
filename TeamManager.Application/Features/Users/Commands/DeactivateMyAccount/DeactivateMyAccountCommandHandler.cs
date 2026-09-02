using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Users.Commands.DeactivateMyAccount
{
    public sealed class DeactivateMyAccountCommandHandler(ICurrentUser currentUser, IUserRepository
        userRepository, IUnitOfWork unitOfWork, ITeamRepository teamRepository, IPasswordHasher passwordHasher) : IRequestHandler<DeactivateMyAccountCommand>
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

            user.Deactivate();

            await teamRepository.DeactivateOwnedTeamsAsync(user.Id, cancellationToken);

            await userRepository.RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}