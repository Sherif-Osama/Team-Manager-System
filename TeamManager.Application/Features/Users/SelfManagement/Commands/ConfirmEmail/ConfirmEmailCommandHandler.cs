using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;
using TeamManager.Application.Abstractions.Security;
using TeamManager.Application.Common.Exceptions;
using TeamManager.Application.Features.Users.SelfManagement.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler(ICurrentUser currentUser, IUserRepository userRepository,
    IEmailConfirmationTokenService tokenService, IUnitOfWork unitOfWork) : IRequestHandler<ConfirmEmailCommand>
{
    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);

        if (user is null)
            throw new UserNotFoundException(currentUser.UserId.Value);

        var tokenHash = tokenService.HashToken(request.Token);

        user.ConfirmEmail(tokenHash);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}