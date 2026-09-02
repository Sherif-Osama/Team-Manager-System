using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Users.Commands.DeleteMyAccount
{
    public sealed class DeleteMyAccountCommandHandler(ICurrentUser currentUser, IUserRepository userRepository,
        ITeamRepository teamRepository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteMyAccountCommand>
    {
        public async Task Handle(DeleteMyAccountCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var userId = currentUser.UserId.Value;

            var user = await userRepository.GetByIdAsync(userId, cancellationToken);

            if (user is null)
                throw new UserNotFoundException(userId);

            var hasActiveOwnedTeams = await teamRepository.HasActiveOwnedTeamsAsync(userId, cancellationToken);

            if (hasActiveOwnedTeams)
                throw new UserOwnsActiveTeamException(userId);

            user.SoftDelete();

            await userRepository.DeactivateActiveMembershipsAsync(userId, cancellationToken);

            await userRepository.RevokeAllRefreshTokensAsync(userId, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}