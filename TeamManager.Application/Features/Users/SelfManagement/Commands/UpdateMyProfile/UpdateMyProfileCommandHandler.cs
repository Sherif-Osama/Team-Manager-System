using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Common.Exceptions;

namespace TeamManager.Application.Features.Users.SelfManagement.Commands.UpdateMyProfile
{
    public sealed class UpdateMyProfileCommandHandler(ICurrentUser currentUser, IUserRepository userRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<UpdateMyProfileCommand>
    {
        public async Task Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            if (!currentUser.UserId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);

            if (user is null)
                throw new UserNotFoundException(currentUser.UserId.Value);

            user.ChangeDisplayName(request.DisplayName);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}