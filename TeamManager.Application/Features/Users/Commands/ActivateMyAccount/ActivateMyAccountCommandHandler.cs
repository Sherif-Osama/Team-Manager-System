using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Users.Commands.ActivateMyAccount
{
    public sealed class ActivateMyAccountCommandHandler(ICurrentUser currentUser, IUserRepository userRepository, IUnitOfWork unitOfWork)
        : IRequestHandler<ActivateMyAccountCommand>
    {
        public async Task Handle(ActivateMyAccountCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);

            if (user is null)
                throw new UserNotFoundException(currentUser.UserId.Value);

            user.Activate();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}