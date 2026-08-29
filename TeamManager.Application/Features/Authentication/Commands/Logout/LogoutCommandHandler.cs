using MediatR;
using TeamManager.Application.Abstractions.Authentication;
using TeamManager.Application.Abstractions.Persistence;

namespace TeamManager.Application.Features.Authentication.Commands.Logout;

public sealed class LogoutCommandHandler(IUserRepository userRepository, IRefreshTokenService refreshTokenService, IUnitOfWork unitOfWork) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenService.HashToken(request.RefreshToken);

        var refreshToken = await userRepository.GetRefreshTokenByHashAsync(tokenHash, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
            return;

        refreshToken.Revoke();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}